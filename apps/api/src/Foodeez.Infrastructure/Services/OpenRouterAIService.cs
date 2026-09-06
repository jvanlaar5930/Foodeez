using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// OpenRouter - one OpenAI-compatible endpoint in front of most of the model providers there
/// are: https://openrouter.ai
///
/// The reason it earns a class of its own rather than being configured as a
/// <see cref="LocalAIService"/> base URL is the free/paid switch below. That switch is the
/// point of the provider here, and expressing it as "type this model slug by hand" would put
/// the burden of knowing which slugs cost money on whoever is filling in the admin form.
///
/// Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class OpenRouterAIService : AIProviderBase
{
    private const string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";

    /// <summary>
    /// OpenRouter's own router over the models that cost nothing. It picks one per request,
    /// filtering for the features the request needs, and neither prompt nor completion tokens
    /// are billed.
    ///
    /// Which model actually answers therefore changes from call to call, and that is worth
    /// knowing about rather than hiding: it is why no output ceiling is sent by default (see
    /// <see cref="MaxTokensAsync"/>), and why quality varies between two identical requests in
    /// a way it does not on any other provider here.
    /// </summary>
    private const string FreeRouterModel = "openrouter/free";

    /// <summary>Free models queue behind paid traffic, so this is the patient end of normal.</summary>
    private const int DefaultTimeoutSeconds = 120;

    private readonly HttpClient _httpClient;
    private readonly ProviderConfiguration _configuration;

    public OpenRouterAIService(
        HttpClient httpClient,
        ProviderConfiguration configuration,
        ILogger<OpenRouterAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "OpenRouter";

    // ────────────────────────── Configuration ──────────────────────────

    private Task<string?> ResolveAsync(string settingKey, string configKey) =>
        _configuration.ResolveAsync(settingKey, configKey);

    /// <summary>
    /// Whether to route through the free models. On unless someone turns it off, because
    /// "free" is the reason to reach for OpenRouter in the first place and the alternative
    /// spends real money the moment it is switched on.
    /// </summary>
    private async Task<bool> UseFreeModelsAsync()
    {
        var raw = await ResolveAsync("openrouter.useFreeModels", "OpenRouter:UseFreeModels");
        return !bool.TryParse(raw, out var enabled) || enabled;
    }

    /// <summary>
    /// The model slug to send.
    ///
    /// Free mode ignores whatever is in the model field: the two settings would otherwise
    /// contradict each other, and a paid slug silently winning over a switch labelled "use
    /// free models" is the expensive direction to get that wrong in.
    /// </summary>
    private async Task<string> ModelAsync()
    {
        if (await UseFreeModelsAsync())
        {
            return FreeRouterModel;
        }

        return await ResolveAsync("openrouter.model", "OpenRouter:Model")
            ?? throw new InvalidOperationException(
                "OpenRouter is set to use paid models but no model is configured. Set one in " +
                "Admin > Settings (openrouter.model), or turn \"Use Free Models\" back on. " +
                "Slugs look like \"anthropic/claude-sonnet-4.5\"; the catalogue is at " +
                "https://openrouter.ai/models.");
    }

    protected override async ValueTask<TimeSpan> RequestTimeoutAsync() =>
        await _configuration.ResolveTimeoutAsync(
            "openrouter.timeoutSeconds", "OpenRouter:TimeoutSeconds", DefaultTimeoutSeconds);

    /// <summary>
    /// The free router accepts images - it filters for the features a request needs, so a
    /// photo lands on a model that can see one.
    ///
    /// A hand-picked paid slug is a different matter: nothing here can tell whether it is a
    /// vision model, and sending an image to one that is not buys a slow failure. So that case
    /// is opt-in, the same way the local provider handles the same unknown.
    /// </summary>
    protected override async Task<bool> SupportsVisionAsync()
    {
        if (await UseFreeModelsAsync())
        {
            return true;
        }

        var raw = await ResolveAsync("openrouter.supportsVision", "OpenRouter:SupportsVision");
        return bool.TryParse(raw, out var enabled) && enabled;
    }

    protected override string VisionUnsupportedNote =>
        "Photos need a model that can see. Turn on \"Model Supports Images\" in Admin > Settings " +
        "if the OpenRouter model you have chosen is a vision model, or describe the meal instead.";

    /// <summary>
    /// Left unset unless an admin configures one.
    ///
    /// In free mode the router chooses a different model per request, so any number picked
    /// here would be wrong for some of them - too small silently truncates a meal plan, too
    /// large is rejected outright by whichever small model happened to answer. Sending nothing
    /// lets each model fall back to its own default, which is the only value that is right for
    /// all of them.
    /// </summary>
    private async Task<int?> MaxTokensAsync() =>
        await _configuration.ResolveIntAsync("openrouter.maxTokens", "OpenRouter:MaxTokens");

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

    private async Task<string> SendMessagesAsync(object[] messages, CancellationToken ct)
    {
        using var request = await BuildRequestAsync(messages, stream: false);

        using var response = await _httpClient.SendAsync(request, ct);
        await EnsureSuccessAsync(response, ct);

        return ExtractContent(await response.Content.ReadAsStringAsync(ct));
    }

    protected override async IAsyncEnumerable<string> StreamCoreAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        using var request = await BuildRequestAsync(
            [new { role = "user", content = prompt }], stream: true);

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        await EnsureSuccessAsync(response, ct);

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.OpenAiDelta(payload);
            if (text.Length > 0) yield return text;
        }
    }

    private async Task<HttpRequestMessage> BuildRequestAsync(object[] messages, bool stream)
    {
        var apiKey = await ResolveAsync("openrouter.apiKey", "OpenRouter:ApiKey")
            ?? throw new InvalidOperationException(
                "No OpenRouter API key is configured. Set one in Admin > Settings, or supply " +
                "OpenRouter:ApiKey. Free models still need a key - it identifies the account " +
                "the free quota belongs to.");

        var body = new Dictionary<string, object?>
        {
            ["model"] = await ModelAsync(),
            ["stream"] = stream,
            ["messages"] = messages
        };

        // Left out of the body entirely rather than sent as null. AIJson's WhenWritingNull does
        // not reach dictionary values, so assigning null here would put "max_tokens": null on
        // the wire - and OpenRouter hands the request to one of many upstreams, any of which
        // may treat an explicit null as a value to validate rather than as "use your default".
        var maxTokens = await MaxTokensAsync();
        if (maxTokens is not null)
        {
            body["max_tokens"] = maxTokens;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        // How OpenRouter labels the traffic on their dashboards and rankings. Optional, and
        // sent because an unlabelled app is harder to recognise in your own usage history.
        request.Headers.Add("X-Title", "Foodeez");

        return request;
    }

    /// <summary>
    /// Carries OpenRouter's own explanation into the exception instead of dropping it.
    ///
    /// This provider's most likely failure by far is a free-tier rate limit, and the status
    /// code alone does not say whether the limit was per-minute or the daily allowance - the
    /// body does, and the difference decides whether waiting a moment is worth trying.
    /// </summary>
    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"OpenRouter returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(error, 500)}");
    }

    /// <summary>
    /// Standard OpenAI shape. The tolerance for "text" and a bare "content" is deliberate:
    /// in free mode the answering model changes per request, and not every upstream that
    /// OpenRouter fronts normalises its response identically.
    /// </summary>
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
