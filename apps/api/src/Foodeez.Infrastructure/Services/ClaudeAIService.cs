using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Anthropic Claude. Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class ClaudeAIService : AIProviderBase
{
    private const string AnthropicBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string DefaultModel = "claude-sonnet-4-6";

    /// <summary>
    /// Used only when nobody has configured "Claude:MaxTokens". How much room the configured
    /// model actually has is a property of that model, not something to guess at in code;
    /// this is Anthropic's default max output for the current Claude models.
    /// </summary>
    private const int FallbackMaxOutputTokens = 8192;

    /// <summary>
    /// A plate of food can run to a dozen entries, and a truncated answer parses as a meal
    /// with items missing rather than as the failure it is.
    /// </summary>
    private const int ImageMaxOutputTokens = 4096;

    /// <summary>
    /// Anthropic answers a prompt of this size in seconds, so a call still running after two
    /// minutes has gone wrong rather than gone slowly. Editable per deployment all the same.
    /// </summary>
    private const int DefaultTimeoutSeconds = 120;

    private readonly HttpClient _httpClient;
    private readonly ProviderConfiguration _configuration;

    public ClaudeAIService(HttpClient httpClient, ProviderConfiguration configuration, ILogger<ClaudeAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "Claude";

    protected override async ValueTask<TimeSpan> RequestTimeoutAsync() =>
        await _configuration.ResolveTimeoutAsync("claude.timeoutSeconds", "Claude:TimeoutSeconds", DefaultTimeoutSeconds);

    protected override Task<bool> SupportsVisionAsync() => Task.FromResult(true);

    protected override async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        var body = new
        {
            model = await ModelAsync(),
            max_tokens = await MaxOutputTokensAsync(),
            messages = new[] { new { role = "user", content = prompt } }
        };

        return ExtractText(await PostAsync(body, HttpCompletionOption.ResponseContentRead, ct));
    }

    protected override async Task<ParsedMealDto> ReadMealImageAsync(
        byte[] imageData, string? mimeType, CancellationToken ct)
    {
        var body = new
        {
            model = await ModelAsync(),
            max_tokens = ImageMaxOutputTokens,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mimeType ?? "image/jpeg",
                                data = Convert.ToBase64String(imageData)
                            }
                        },
                        new { type = "text", text = MealParsePrompt.BuildImage() }
                    }
                }
            }
        };

        return MealParsePrompt.Parse(ExtractText(await PostAsync(body, HttpCompletionOption.ResponseContentRead, ct)));
    }

    /// <summary>
    /// The same request as <see cref="SendAsync"/> with `stream` set, so the answer arrives in
    /// the pieces Anthropic writes it in rather than in one block at the end.
    /// </summary>
    protected override async IAsyncEnumerable<string> StreamCoreAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct)
    {
        var body = new
        {
            model = await ModelAsync(),
            max_tokens = await MaxOutputTokensAsync(),
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var response = await SendRequestAsync(body, HttpCompletionOption.ResponseHeadersRead, ct);

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.Read(payload, root =>
                root.TryGetProperty("type", out var type) &&
                type.GetString() == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

    private async Task<string> ModelAsync() =>
        await _configuration.ResolveAsync("claude.model", "Claude:Model") ?? DefaultModel;

    private async Task<int> MaxOutputTokensAsync() =>
        await _configuration.ResolveIntAsync("claude.maxTokens", "Claude:MaxTokens") ?? FallbackMaxOutputTokens;

    private async Task<string> PostAsync(object body, HttpCompletionOption completion, CancellationToken ct)
    {
        using var response = await SendRequestAsync(body, completion, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Builds the request, putting the credentials on the message rather than on the client.
    ///
    /// This used to set DefaultRequestHeaders in the constructor, which mutates a client from
    /// the shared IHttpClientFactory pool - and read the API key once, so rotating it needed a
    /// restart. Per-request headers avoid both, and mean the streaming path no longer depends
    /// on a side effect of construction to be authenticated.
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestAsync(
        object body, HttpCompletionOption completion, CancellationToken ct)
    {
        var apiKey = await _configuration.ResolveAsync("claude.apiKey", "Claude:ApiKey")
            ?? throw new InvalidOperationException(
                "No Claude API key is configured. Set one in Admin > Settings, or supply Claude:ApiKey.");

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicBaseUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);

        var response = await _httpClient.SendAsync(request, completion, ct);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private static string ExtractText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);

        if (doc.RootElement.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.Array &&
            content.GetArrayLength() > 0 &&
            content[0].TryGetProperty("text", out var text))
        {
            return text.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
