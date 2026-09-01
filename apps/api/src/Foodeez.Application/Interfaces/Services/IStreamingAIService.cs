namespace Foodeez.Application.Interfaces.Services;

/// <summary>
/// A provider that can hand a model's answer back as it is written rather than at the end.
///
/// Kept separate from <see cref="IAIService"/> because it is a transport capability, not a
/// feature: it takes a raw prompt and returns raw text, and the feature-shaped methods on
/// IAIService stay the way anything that just wants an answer asks for one.
/// </summary>
public interface IStreamingAIService
{
    /// <summary>
    /// Yields the model's answer in the pieces it arrives in. The pieces concatenate to
    /// exactly the text the non-streaming call would have returned.
    /// </summary>
    IAsyncEnumerable<string> StreamAsync(string prompt, CancellationToken ct = default);
}
