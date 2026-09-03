using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Common;
using Microsoft.AspNetCore.Http.Features;

namespace Foodeez.API.Streaming;

/// <summary>
/// Writes a use case's AI stream to the response as server-sent events.
///
/// Once the first byte is out the status code is settled, so a failure part-way through
/// cannot become a 500 - it goes down the same channel as an "error" event, which is what the
/// client is watching for anyway.
/// </summary>
internal static class ServerSentEventStream
{
    private static readonly JsonSerializerOptions EventJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static async Task WriteAsync(
        HttpResponse response,
        IAsyncEnumerable<AIStreamEvent> events,
        CancellationToken ct)
    {
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        // Proxies buffer a response body by default, which holds every token back until the
        // model has finished - the one thing this endpoint exists to avoid.
        response.Headers["X-Accel-Buffering"] = "no";
        response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var logger = response.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ServerSentEventStream));

        try
        {
            await foreach (var streamEvent in events.WithCancellation(ct))
            {
                await SendAsync(response, streamEvent, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Genuinely ambiguous: this fires both when the reader closes the tab (nothing to
            // report) and when the transport itself gives up on a connection that stayed idle
            // too long - Kestrel's data-rate limit or a reverse proxy's read timeout - which
            // looks identical from here but is an operational problem worth knowing about. A
            // real client abort will be common in practice, so this stays a warning, not an
            // error, but it beats what was here before: complete silence either way.
            logger.LogWarning(
                "An AI stream ended via cancellation before finishing - either the client " +
                "disconnected, or the connection was dropped for being idle too long.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An AI stream failed part-way through.");

            // CancellationToken.None: the token may be the reason we are here, and the client
            // that is still listening deserves to be told why the text stopped.
            await SendAsync(
                response,
                AIStreamEvent.Error("The AI service stopped responding. Please try again in a moment."),
                CancellationToken.None);
        }
    }

    private static async Task SendAsync(HttpResponse response, AIStreamEvent streamEvent, CancellationToken ct)
    {
        await response.WriteAsync($"data: {JsonSerializer.Serialize(streamEvent, EventJson)}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}
