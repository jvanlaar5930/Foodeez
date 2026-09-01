namespace Foodeez.Application.Common;

/// <summary>
/// The AI provider could not produce a usable result. Distinct from a validation or
/// not-found failure: nothing about the request was wrong, so the caller should be told to
/// try again rather than to change what they asked for.
/// </summary>
public class AIGenerationFailedException : Exception
{
    public AIGenerationFailedException(string message) : base(message) { }
}
