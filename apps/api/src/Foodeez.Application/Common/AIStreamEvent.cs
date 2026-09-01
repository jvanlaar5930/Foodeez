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
}
