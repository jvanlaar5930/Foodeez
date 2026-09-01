using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.Common;

/// <summary>
/// Turns a model's answer into something worth watching arrive.
///
/// Every AI feature here ends in a JSON object the app has to parse, and watching raw JSON
/// type itself out is worse than watching nothing. So the prompts ask for a few sentences of
/// plain assessment first and the JSON after it: the prose is what streams to the screen, and
/// the JSON is read from the accumulated text once the model has finished.
/// </summary>
public static class AINarration
{
    /// <summary>Appended to a prompt that will be streamed to someone watching.</summary>
    public const string Instruction =
        "Before the JSON, write 2-4 short sentences of your assessment in plain language, " +
        "addressed to the person you are advising - this is shown to them as you write it. " +
        "Then output the JSON object on its own, with nothing after it. " +
        "Do not use markdown, code fences or headings anywhere in your answer.";

    /// <summary>
    /// Streams <paramref name="prompt"/>, yielding only the prose that leads the answer while
    /// collecting the whole thing into <paramref name="transcript"/> for parsing afterwards.
    /// </summary>
    public static async IAsyncEnumerable<string> NarrateAsync(
        IStreamingAIService ai,
        string prompt,
        StringBuilder transcript,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var reachedJson = false;

        await foreach (var chunk in ai.StreamAsync(prompt, ct))
        {
            if (chunk.Length == 0)
            {
                continue;
            }

            transcript.Append(chunk);

            if (reachedJson)
            {
                continue;
            }

            // The first brace is where the prose ends and the machine-readable part begins.
            var brace = chunk.IndexOf('{');
            if (brace < 0)
            {
                yield return chunk;
                continue;
            }

            reachedJson = true;
            if (brace > 0)
            {
                yield return chunk[..brace];
            }
        }
    }
}
