using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Google Gemini. Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class GeminiAIService : AIProviderBase
{
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    // Google retires these on their own schedule - gemini-1.5-flash and then 2.5-flash both
    // started 404ing, taking every AI feature down with them - and an overloaded model 503s
    // for hours at a time. Keep it in configuration so the next one is a settings change
    // rather than a deploy.
    private const string DefaultModel = "gemini-3.6-flash";

    private const int MaxOutputTokens = 8192;

    /// <summary>Low, because every prompt here asks for JSON rather than for invention.</summary>
    private const double Temperature = 0.3;

    /// <summary>A hosted model that has not answered in two minutes is not about to.</summary>
    private const int DefaultTimeoutSeconds = 120;

    private readonly HttpClient _httpClient;
    private readonly ProviderConfiguration _configuration;

    public GeminiAIService(HttpClient httpClient, ProviderConfiguration configuration, ILogger<GeminiAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "Gemini";

    protected override async ValueTask<TimeSpan> RequestTimeoutAsync() =>
        await _configuration.ResolveTimeoutAsync("gemini.timeoutSeconds", "Gemini:TimeoutSeconds", DefaultTimeoutSeconds);

    protected override Task<bool> SupportsVisionAsync() => Task.FromResult(true);

    protected override async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = MaxOutputTokens, temperature = Temperature }
        };

        using var response = await PostAsync("generateContent", body, HttpCompletionOption.ResponseContentRead, ct);
        return ReadText(await response.Content.ReadAsStringAsync(ct));
    }

    protected override async Task<ParsedMealDto> ReadMealImageAsync(
        byte[] imageData, string? mimeType, CancellationToken ct)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inlineData = new
                            {
                                mimeType = mimeType ?? "image/jpeg",
                                data = Convert.ToBase64String(imageData)
                            }
                        },
                        new { text = MealParsePrompt.BuildImage() }
                    }
                }
            }
        };

        using var response = await PostAsync("generateContent", body, HttpCompletionOption.ResponseContentRead, ct);
        return MealParsePrompt.Parse(ReadText(await response.Content.ReadAsStringAsync(ct)));
    }

    /// <summary>Google's streaming endpoint, asked for as server-sent events rather than a JSON array.</summary>
    protected override async IAsyncEnumerable<string> StreamCoreAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = MaxOutputTokens, temperature = Temperature }
        };

        using var response = await PostAsync(
            "streamGenerateContent?alt=sse", body, HttpCompletionOption.ResponseHeadersRead, ct);

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.Read(payload, root =>
                root.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

    /// <summary>
    /// Posts to one of Gemini's endpoints and checks the result.
    ///
    /// The status check is deliberately not EnsureSuccessStatusCode: that discards the body,
    /// and the body is where Google explains itself - "model is overloaded", "no longer
    /// available, use X". Without it a caller sees a bare 503 and cannot tell a temporary
    /// spike from a model that has been retired out from under them.
    /// </summary>
    private async Task<HttpResponseMessage> PostAsync(
        string endpoint, object body, HttpCompletionOption completion, CancellationToken ct)
    {
        var model = await _configuration.ResolveAsync("gemini.model", "Gemini:Model") ?? DefaultModel;
        var apiKey = await _configuration.ResolveAsync("gemini.apiKey", "Gemini:ApiKey")
            ?? throw new InvalidOperationException(
                "No Gemini API key is configured. Set one in Admin > Settings, or supply Gemini:ApiKey.");

        var separator = endpoint.Contains('?') ? '&' : '?';
        var url = $"{ApiBase}/{model}:{endpoint}{separator}key={apiKey}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request, completion, ct);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var status = (int)response.StatusCode;
        var error = await response.Content.ReadAsStringAsync(ct);
        response.Dispose();

        if (error.Length > 500) error = error[..500] + "...";
        throw new HttpRequestException($"Gemini returned {status} for model '{model}': {error}");
    }

    private static string ReadText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }
}
