using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/wh/{b64Signature}/{b64url}/{b64scheme}", async (string b64Signature, string b64url, string b64scheme,
    JsonDocument payload, IHttpClientFactory httpClientFactory) =>
{
    // Check signature
    var signature = SafeUrlBase64DecodeData(b64Signature);
    var hmac = GetHmac(app.Configuration["hmacKey"], b64url, b64scheme);
    if (!signature.SequenceEqual(hmac))
    {
        return Results.Unauthorized();
    }

    var url = SafeUrlBase64Decode(b64url);
    var scheme = SafeUrlBase64Decode(b64scheme);

    // Replace
    Regex regex = new Regex(@"\$\{\{(.*?(?:\\\})*?)\}\}");
    // \$\{\{(.*?(?:\})*)\}\} matchea bien pero en bloque
    var replacements = regex.Matches(scheme).Reverse();
    foreach (Match replacement in replacements)
    {
        var components = replacement.Groups[1].Value.Split("??", 2);

        var value = GetValue(payload, components[0]) ?? (components.Length == 2 ? components[1].Replace("\\{","{").Replace("\\}","}"): @"null");
        scheme = scheme.Replace(replacement.Groups[0].Value, value);
    }

    // Call the final Endpoint
    var httpClient = httpClientFactory.CreateClient();
    var post = await httpClient.PostAsync(url, new StringContent(scheme, Encoding.UTF8, "application/json"));
    if (!post.IsSuccessStatusCode)
    {
        return Results.Problem();
    }

    return Results.Stream(await post.Content.ReadAsStreamAsync(), "application/json");

    //return Results.Ok();
}).WithName("webhook");

app.MapPost("/api/wh", async (CreateUrlDto createUrlDto) =>
{
    // Data
    var url = SafeUrlBase64Encode(createUrlDto.url);
    var scheme = SafeUrlBase64Encode(createUrlDto.scheme);

    // Generate signature
    var hmac = GetHmac(createUrlDto.hmacKey, url, scheme);
    var signature = SafeUrlBase64EncodeData(hmac);

    var finalUrl = $"/wh/{signature}/{url}/{scheme}";
    return Results.Ok(finalUrl);
}).WithName("createWebHook");


app.Run();

/// <summary>
/// Get the raw property value or -null-
/// </summary>
string? GetValue(JsonDocument json, string path)
{
    var element = json.RootElement;

    var pathSegments = path.Split('.');
    foreach (var segment in pathSegments)
    {
        if(!element.TryGetProperty(segment, out element))
        {
            return null;
        }
    }
    return element.GetRawText();
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
    => HMACSHA256.HashData(SafeUrlBase64DecodeData(hmacKey), Encoding.UTF8.GetBytes(b64Url + b64Scheme));

public record CreateUrlDto(string hmacKey, string url, string scheme);

// Make the implicit Program class public so test projects can access it
public partial class Program { }