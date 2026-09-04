using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Local Ollama - runs on the same machine, completely free.
///
/// Everything except the HTTP call lives in <see cref="AIProviderBase"/>.
/// </summary>
public sealed class OllamaAIService : AIProviderBase
{
    private const string DefaultBaseUrl = "http://localhost:11434";
    private const string DefaultModel = "llama3";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OllamaAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaAIService> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    protected override string ProviderName => "Ollama";

    protected override string VisionUnsupportedNote =>
        "Photos need a provider that can see - this Ollama model has no vision. Describe the meal instead.";

    protected override async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        using var request = BuildRequest(prompt, stream: false);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>Ollama streams bare JSON objects, one per line, rather than server-sent events.</summary>
    public override async IAsyncEnumerable<string> StreamAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = BuildRequest(prompt, stream: true);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var line in StreamingHttp.ReadJsonLinesAsync(response, ct))
        {
            var text = StreamingHttp.Read(line, root =>
                root.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

    private HttpRequestMessage BuildRequest(string prompt, bool stream)
    {
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? DefaultBaseUrl;

        var body = new
        {
            model = _configuration["Ollama:Model"] ?? DefaultModel,
            stream,
            messages = new[] { new { role = "user", content = prompt } }
        };

        return new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/chat")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body, AIJson.Options), Encoding.UTF8, "application/json")
        };
    }
}
