using Microsoft.Extensions.Options;
using System.Buffers.Text;
using System.Collections;
using System.Dynamic;
using System.Net;
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
    var signature = Convert.FromBase64String(b64Signature);
    var hmac = GetHmac(app.Configuration["hmacKey"], b64url, b64scheme);
    if (!signature.SequenceEqual(hmac))
    {
        return Results.Unauthorized();
    }

    var url = Encoding.UTF8.GetString(Convert.FromBase64String(b64url));
    var scheme = Encoding.UTF8.GetString(Convert.FromBase64String(b64scheme));

    // Replace
    Regex regex = new Regex(@"\$\{\{(.*?)\}\}");
    var replacements = regex.Matches(scheme).Reverse();
    foreach (Match replacement in replacements)
    {
        var value = GetValue(payload, replacement.Groups[1].Value);
        scheme = scheme.Replace(replacement.Groups[0].Value, value);
    }

    // Call the final Endpoint
    var httpClient = httpClientFactory.CreateClient();
    var post = await httpClient.PostAsync(url, new StringContent(scheme, Encoding.UTF8, "application/json"));
    if (!post.IsSuccessStatusCode)
    {
        return Results.Problem();
    }

    return Results.Stream(await post.Content.ReadAsStreamAsync());

    //return Results.Ok();
}).WithName("webhook");

app.MapPost("/api/wh", async (CreateUrlDto createUrlDto) =>
{
    // Data
    var url = Convert.ToBase64String(Encoding.UTF8.GetBytes(createUrlDto.url));
    var scheme = Convert.ToBase64String(Encoding.UTF8.GetBytes(createUrlDto.scheme));

    // Generate signature
    var hmac = GetHmac(app.Configuration["hmacKey"], url, scheme);
    var signature = Convert.ToBase64String(hmac);

    var finalUrl = $"/wh/{signature}/{url}/{scheme}";
    return Results.Ok(finalUrl);
}).WithName("create");


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
            // return String.Empty;
            return @"null";
        }
    }
    return element.GetRawText();
}

byte[] GetHmac(string hmacKey, string b64Url, string b64Scheme)
    => HMACSHA256.HashData(Encoding.UTF8.GetBytes(hmacKey), Encoding.UTF8.GetBytes(b64Url + b64Scheme));

public record CreateUrlDto(string hmacKey, string url, string scheme);

// Make the implicit Program class public so test projects can access it
public partial class Program { }