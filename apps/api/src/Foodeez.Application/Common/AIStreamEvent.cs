namespace Foodeez.Application.Common;

/// <summary>
/// One message on the wire while an AI answer is being produced: a piece of text as it is
/// written, the finished result, or the news that it could not be produced.
/// </summary>
public class AIStreamEvent
{
    public string Type { get; private init; } = "delta";

    /// <summary>The next piece of prose, on a "delta" event.</summary>
    public string? Text { get; private init; }

    /// <summary>The finished, parsed result, on a "result" event.</summary>
    public object? Data { get; private init; }

    /// <summary>Why nothing could be produced, on an "error" event.</summary>
    public string? Message { get; private init; }

    public static AIStreamEvent Delta(string text) => new() { Type = "delta", Text = text };

    public static AIStreamEvent Result(object data) => new() { Type = "result", Data = data };

    public static AIStreamEvent Error(string message) => new() { Type = "error", Message = message };

    /// <summary>
    /// How far along a piece of work in several steps has got. A week's meal plan is written a
    /// day at a time, and "day 4 of 7" is the difference between a wait somebody can sit
    /// through and one that looks like it has hung.
    /// </summary>
    public static AIStreamEvent Progress(object data) => new() { Type = "progress", Data = data };

    /// <summary>
    /// One finished piece, saved and ready to show before the rest arrives. The calendar fills
    /// in a day at a time this way, which is also what makes a failure halfway through cost
    /// only the days that had not been written yet.
    /// </summary>
    public static AIStreamEvent Part(object data) => new() { Type = "part", Data = data };
}
