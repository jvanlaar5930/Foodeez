using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// OpenAI-compatible API via Groq (free tier) - https://console.groq.com
///
/// Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class GroqAIService : AIProviderBase
{
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string DefaultModel = "llama-3.1-8b-instant";

    /// <summary>
    /// Used only when nobody has configured "Groq:MaxTokens". How much room the configured
    /// model actually has is a property of that model, not something to guess at in code.
    /// </summary>
    private const int FallbackMaxOutputTokens = 8192;

    /// <summary>Groq's whole selling point is speed; a call this slow has stalled, not queued.</summary>
    private const int DefaultTimeoutSeconds = 60;

    private readonly HttpClient _httpClient;
    private readonly ProviderConfiguration _configuration;

    public GroqAIService(HttpClient httpClient, ProviderConfiguration configuration, ILogger<GroqAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "Groq";

    protected override async ValueTask<TimeSpan> RequestTimeoutAsync() =>
        await _configuration.ResolveTimeoutAsync("groq.timeoutSeconds", "Groq:TimeoutSeconds", DefaultTimeoutSeconds);

    protected override string VisionUnsupportedNote =>
        "Photos need a provider that can see - Groq's free tier has no vision model. Describe the meal instead.";

    protected override async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        using var request = await BuildRequestAsync(prompt, stream: false);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>Groq speaks the OpenAI streaming protocol, so the frames read the same way.</summary>
    protected override async IAsyncEnumerable<string> StreamCoreAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = await BuildRequestAsync(prompt, stream: true);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.OpenAiDelta(payload);
            if (text.Length > 0) yield return text;
        }
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(string prompt, bool stream)
    {
        var apiKey = await _configuration.ResolveAsync("groq.apiKey", "Groq:ApiKey")
            ?? throw new InvalidOperationException(
                "No Groq API key is configured. Set one in Admin > Settings, or supply Groq:ApiKey.");

        var body = new
        {
            model = await _configuration.ResolveAsync("groq.model", "Groq:Model") ?? DefaultModel,
            max_tokens = await MaxOutputTokensAsync(),
            stream,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return request;
    }

    private async Task<int> MaxOutputTokensAsync() =>
        await _configuration.ResolveIntAsync("groq.maxTokens", "Groq:MaxTokens") ?? FallbackMaxOutputTokens;
}
