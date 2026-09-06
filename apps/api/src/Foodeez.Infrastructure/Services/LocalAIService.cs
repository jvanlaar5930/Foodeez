using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Any self-hosted server that speaks the OpenAI chat-completions API: LM Studio, llama.cpp
/// (llama-server), vLLM, LocalAI, Jan, KoboldCpp, text-generation-webui - and Ollama through
/// its /v1 endpoint. They differ only in the port and the model name, so one client covers
/// them all.
///
/// Configuration comes from the admin settings table first (local.*), falling back to
/// appsettings (LocalAI:*), so the server can be pointed somewhere else without a redeploy.
/// Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class LocalAIService : AIProviderBase
{
    private const string DefaultBaseUrl = "http://localhost:1234/v1";   // LM Studio's default
    private const string DefaultModel = "local-model";
    private const int DefaultTimeoutSeconds = 300;

    private readonly HttpClient _httpClient;
    private readonly ProviderConfiguration _configuration;

    public LocalAIService(
        HttpClient httpClient,
        ProviderConfiguration configuration,
        ILogger<LocalAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "Local LLM";

    // Names the switch as it is labelled in the admin UI rather than as the settings key it is
    // stored under: the person reading this has just taken a photograph, and "local.supportsVision"
    // is not something they can go and find.
    protected override string VisionUnsupportedNote =>
        "Photos are switched off for the local model. Turn on \"Local Model Supports Images\" in " +
        "Admin > Settings once a vision model is loaded, or describe the meal instead.";

    // ────────────────────────── Configuration ──────────────────────────

    /// <summary>
    /// Admin-editable settings win; appsettings is the fallback for a machine with no rows yet.
    /// The lookup itself now lives in <see cref="ProviderConfiguration"/>, shared with every
    /// other provider - this stays only to keep the call sites below reading the same way.
    /// </summary>
    private Task<string?> ResolveAsync(string settingKey, string configKey) =>
        _configuration.ResolveAsync(settingKey, configKey);

    /// <summary>
    /// Accepts whatever shape the admin pasted - "http://localhost:8080",
    /// "http://localhost:1234/v1", or the full ".../v1/chat/completions" - and returns the
    /// chat-completions endpoint.
    /// </summary>
    internal static string BuildChatCompletionsUrl(string baseUrl)
    {
        var url = baseUrl.Trim().TrimEnd('/');

        if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return $"{url}/chat/completions";

        return $"{url}/v1/chat/completions";
    }

    /// <summary>
    /// Sending an image to a text-only model only buys a slow failure, so it is opt-in - and
    /// it is a runtime fact about whatever model happens to be loaded, not about this class.
    /// </summary>
    protected override async Task<bool> SupportsVisionAsync()
    {
        var raw = await ResolveAsync("local.supportsVision", "LocalAI:SupportsVision");
        return bool.TryParse(raw, out var enabled) && enabled;
    }

    protected override async ValueTask<TimeSpan> RequestTimeoutAsync() =>
        await _configuration.ResolveTimeoutAsync("local.timeoutSeconds", "LocalAI:TimeoutSeconds", DefaultTimeoutSeconds);

    /// <summary>
    /// How much a loaded model can actually produce in one response is not something we can
    /// learn from an OpenAI-compatible server - "/v1/models" does not carry it, and the model
    /// picked in LM Studio's UI never reaches this app at all. Rather than guess a number and
    /// risk it being too small for whatever is loaded (silently truncating a large plan) or
    /// rejected as too large for a small one, this is left unset unless an admin explicitly
    /// configures it - the server then falls back to its own per-model default.
    /// </summary>
    private async Task<int?> MaxTokensAsync()
    {
        var raw = await ResolveAsync("local.maxTokens", "LocalAI:MaxTokens");
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : null;
    }

    // ────────────────────────── Transport ──────────────────────────

    protected override Task<string> SendAsync(string prompt, CancellationToken ct) =>
        SendMessagesAsync([new { role = "user", content = prompt }], ct);

    protected override async Task<ParsedMealDto> ReadMealImageAsync(
        byte[] imageData, string? mimeType, CancellationToken ct)
    {
        var dataUri = $"data:{mimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(imageData)}";

        object[] messages =
        [
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = dataUri } },
                    new { type = "text", text = MealParsePrompt.BuildImage() }
                }
            }
        ];

        return MealParsePrompt.Parse(await SendMessagesAsync(messages, ct));
    }

    /// <remarks>
    /// The deadline used to be applied here, and separately again in the streaming method
    /// below. It now comes from <see cref="AIProviderBase"/>, which links it onto the caller's
    /// token before either of these is entered - so the caller going away still ends the
    /// request, and no provider can be added later that forgets to set one.
    /// </remarks>
    private async Task<string> SendMessagesAsync(object[] messages, CancellationToken ct)
    {
        var baseUrl = await ResolveAsync("local.baseUrl", "LocalAI:BaseUrl") ?? DefaultBaseUrl;
        using var request = await BuildRequestAsync(baseUrl, messages, stream: false);

        using var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Local LLM at {baseUrl} returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(error, 500)}");
        }

        return ExtractContent(await response.Content.ReadAsStringAsync(ct));
    }

    protected override async IAsyncEnumerable<string> StreamCoreAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        var baseUrl = await ResolveAsync("local.baseUrl", "LocalAI:BaseUrl") ?? DefaultBaseUrl;
        using var request = await BuildRequestAsync(
            baseUrl, [new { role = "user", content = prompt }], stream: true);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.OpenAiDelta(payload);
            if (text.Length > 0) yield return text;
        }
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(string baseUrl, object[] messages, bool stream)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = await ResolveAsync("local.model", "LocalAI:Model") ?? DefaultModel,
            ["max_tokens"] = await MaxTokensAsync(),
            ["stream"] = stream,
            ["messages"] = messages
        };

        var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUrl(baseUrl))
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };

        // Most local servers ignore the key; some (vLLM, a proxied LM Studio) require one.
        var apiKey = await ResolveAsync("local.apiKey", "LocalAI:ApiKey");
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        return request;
    }

    /// <summary>Standard OpenAI shape; a few servers answer with "text" or a bare "content" instead.</summary>
    private static string ExtractContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;

            if (first.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("content", out var bare))
            return bare.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
