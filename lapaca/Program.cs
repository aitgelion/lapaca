using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Http clients:
builder.Services.AddHttpClient();
builder.Services.AddHttpClient("NoCert")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Configuration["enableOpenApi"]?.ToLower()=="true")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.MapPost("/wh/{b64Signature}/{b64Conf}", async (HttpRequest request, string b64Signature, string b64Conf,
    JsonDocument payload, IHttpClientFactory httpClientFactory) =>
{
    // Check signature
    var signature = SafeUrlBase64DecodeData(b64Signature);
    var hmac = GetHmacData(app.Configuration["hmacKey"], b64Conf);
    if (!signature.SequenceEqual(hmac))
    {
        return Results.Unauthorized();
    }

    if (request.Query.TryGetValue("logPayload", out var _))
    {
        app.Logger.LogInformation(JsonSerializer.Serialize(payload));
    }
    var logSentPayload = request.Query.TryGetValue("logSentPayload", out var _);

    // Get WebHook Configuration
    WebHookConfig? config;
    using (var deflated = new MemoryStream(b64Conf.Length))
    using (var inflater = new DeflateStream(new MemoryStream(SafeUrlBase64DecodeData(b64Conf)), CompressionMode.Decompress))
    {
        inflater.CopyTo(deflated);
        deflated.Seek(0, SeekOrigin.Begin);

        config = ProtoBuf.Serializer.Deserialize<WebHookConfig>(deflated);
    }

    return await ProcessWebHook(request.Headers, httpClientFactory, config, payload, logSentPayload);
}).WithName("WebHookExCompressed");

app.MapPost("/wh/{b64Signature}/{b64url}/{b64scheme}", async (HttpRequest request, string b64Signature, string b64url, string b64scheme,
    JsonDocument payload, IHttpClientFactory httpClientFactory) =>
{
    // Check signature
    var signature = SafeUrlBase64DecodeData(b64Signature);
    var hmac = GetHmac(app.Configuration["hmacKey"], b64url, b64scheme);
    if (!signature.SequenceEqual(hmac))
    {
        return Results.Unauthorized();
    }

    if (request.Query.TryGetValue("logPayload", out var _))
    {
        app.Logger.LogInformation(JsonSerializer.Serialize(payload));
    }
    var logSentPayload = request.Query.TryGetValue("logSentPayload", out var _);

    var url = SafeUrlBase64Decode(b64url);
    var scheme = SafeUrlBase64Decode(b64scheme);
    
    WebHookConfig config = new WebHookConfig("", url, scheme);

    return await ProcessWebHook(request.Headers, httpClientFactory, config, payload, logSentPayload);
}).WithName("webhook");

// API
app.MapPost("/api/wh", (WebHookConfig createUrlDto) =>
{
    // Data
    var url = SafeUrlBase64Encode(createUrlDto.Url);
    var scheme = SafeUrlBase64Encode(createUrlDto.Scheme);

    // Generate signature
    var hmac = GetHmac(createUrlDto.Hmac, url, scheme);
    var signature = SafeUrlBase64EncodeData(hmac);
    
    // Simple URL
    var finalUrl = $"/wh/{signature}/{url}/{scheme}";

    return Results.Ok(finalUrl);
}).WithName("createWebHook");

app.MapPost("/api/whex", (WebHookConfig createUrlDto) =>
{
    using (var deflated = new MemoryStream())
    {
        using (var compressor = new DeflateStream(deflated, CompressionMode.Compress, true))
        {
            ProtoBuf.Serializer.Serialize(compressor, createUrlDto);
        }

        var conf = SafeUrlBase64EncodeData(deflated.ToArray());
        var signature = SafeUrlBase64EncodeData(GetHmacData(createUrlDto.Hmac, conf));

        var finalUrl = $"/wh/{signature}/{conf}";
        return Results.Ok(finalUrl);
    }
}).WithName("createWebHookEx");


app.Run();

/// <summary>
/// Get the raw property value or -null-
/// </summary>
string? GetValue(JsonDocument json, string path)
{
    var arrayElement = new Regex("(.*?)\\[(\\d+)\\]");


    var element = json.RootElement;

    //var pathSegments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
    var separator = new Regex("(?<!\\\\)\\.");
    var pathSegments = separator.Split(path).Select(s => s.Replace("\\.", "."));

    foreach (var segment in pathSegments)
    {
        var isArrayElement = arrayElement.Match(segment);

        if (!element.TryGetProperty(isArrayElement.Success ? isArrayElement.Groups[1].Value : segment, out element))
        {
            return null;
        }

        if (isArrayElement.Success)
        {
            if (element.ValueKind == JsonValueKind.Array && int.TryParse(isArrayElement.Groups[2].Value, out var i))
            {
                try
                {
                    element = element.EnumerateArray().ElementAt(i);
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
    return element.GetRawText();
}

async Task<IResult> ProcessWebHook(IHeaderDictionary requestHeaders, IHttpClientFactory httpClientFactory, WebHookConfig config, JsonDocument payload, bool logPayload = false)
{
    // Replace
    string finalPayload = config.Scheme;
    Regex regex = new Regex(@"\$\{\{(.*?(?:\\\})*?)\}\}");
    var replacements = regex.Matches(config.Scheme).Reverse();
    foreach (Match replacement in replacements)
    {
        var components = replacement.Groups[1].Value.Split("??", 2);

        var value = GetValue(payload, components[0]) ?? (components.Length == 2 ? components[1].Replace("\\{", "{").Replace("\\}", "}") : @"null");
        finalPayload = finalPayload.Replace(replacement.Groups[0].Value, value.TrimStart('"').TrimEnd('"'));
    }


    if (logPayload)
    {
        app.Logger.LogInformation(finalPayload);
    }


    // Call the final endpoint
    var httpClient = config.SkipCertVerification ?
        httpClientFactory.CreateClient("NoCert") :
        httpClientFactory.CreateClient();

    var authHeader = requestHeaders.Authorization.FirstOrDefault()?.Split(' ', 2);
    if (authHeader?.Length > 0)
    {
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authHeader[0], authHeader.Length > 1 ? authHeader[1] : null);
    }

    var post = await httpClient.PostAsync(config.Url, new StringContent(finalPayload, Encoding.UTF8, "application/json"));
    if (!post.IsSuccessStatusCode)
    {
        return Results.Problem();
    }

    return Results.Stream(await post.Content.ReadAsStreamAsync(), "application/json");
}

string SafeUrlBase64EncodeData(byte[] data)
  => Convert.ToBase64String(data).Replace("+", "-").Replace("/", "_");

string SafeUrlBase64Encode(string str)
  => SafeUrlBase64EncodeData(Encoding.UTF8.GetBytes(str));

byte[] SafeUrlBase64DecodeData(string str)
  => Convert.FromBase64String(str.Replace("-", "+").Replace("_", "/"));

string SafeUrlBase64Decode(string str)
  => Encoding.UTF8.GetString(SafeUrlBase64DecodeData(str));

byte[] GetHmac(string hmacKey, string b64Url, string b64Scheme)
    => GetHmacData(hmacKey, b64Url + b64Scheme);

byte[] GetHmacData(string hmacKey, string b64Data)
    => HMACSHA256.HashData(SafeUrlBase64DecodeData(hmacKey), Encoding.UTF8.GetBytes(b64Data));

public record WebHookConfig(
    [property: ProtoBuf.ProtoIgnore]    string Hmac,
    [property: ProtoBuf.ProtoMember(1)] string Url,
    [property: ProtoBuf.ProtoMember(2)] string Scheme,
    [property: ProtoBuf.ProtoMember(3)] bool SkipCertVerification = false
);

// Make the implicit Program class public so test projects can access it
public partial class Program { }
