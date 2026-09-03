namespace Foodeez.Application.Common;

/// <summary>
/// A save reported that it changed fewer rows than expected - typically the database
/// provider's own retry-on-transient-failure re-running a save whose first attempt actually
/// already committed. Kept provider-agnostic so use cases can react to it (usually by
/// redoing an idempotent operation) without the Application layer depending on EF Core.
/// </summary>
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
