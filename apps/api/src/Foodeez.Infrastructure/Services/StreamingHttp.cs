using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Reading a model's answer off the wire as it is written. Providers agree on almost nothing
/// here - Anthropic and Google send server-sent events, OpenAI-compatible servers send them
/// with a different payload, Ollama sends bare JSON lines - so the reading of the frames
/// lives here and each provider only says how to find the text inside one.
/// </summary>
internal static class StreamingHttp
{
    /// <summary>The payload of each `data:` line, skipping keep-alives and the closing sentinel.</summary>
    public static async IAsyncEnumerable<string> ReadServerSentEventsAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line[5..].Trim();
            if (payload.Length == 0 || payload == "[DONE]")
            {
                continue;
            }

            yield return payload;
        }
    }

    /// <summary>Each non-empty line, for servers that stream bare JSON objects one per line.</summary>
    public static async IAsyncEnumerable<string> ReadJsonLinesAsync(
        HttpResponseMessage response,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.Trim().Length > 0)
            {
                yield return line;
            }
        }
    }

    /// <summary>The text of an OpenAI-compatible chunk: choices[0].delta.content.</summary>
    public static string OpenAiDelta(string payload) =>
        Read(payload, root =>
            root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0 &&
            choices[0].TryGetProperty("delta", out var delta) &&
            delta.TryGetProperty("content", out var content) &&
            content.ValueKind == JsonValueKind.String
                ? content.GetString()
                : null);

    /// <summary>
    /// Runs a reader over one streamed frame. A frame that is not what we are looking for -
    /// a provider's own bookkeeping, a truncated line - contributes no text rather than
    /// killing the stream halfway through an answer.
    /// </summary>
    public static string Read(string payload, Func<JsonElement, string?> readText)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            return readText(doc.RootElement) ?? string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }
}
