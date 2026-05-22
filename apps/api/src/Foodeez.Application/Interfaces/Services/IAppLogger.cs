namespace Foodeez.Application.Interfaces.Services;

public interface IAppLogger
{
    void LogDebug(string message, string? source = null);
    void LogInfo(string message, string? source = null, string? userId = null);
    void LogWarning(string message, string? source = null, string? userId = null);
    void LogError(
        string message,
        Exception? exception = null,
        string? source = null,
        string? requestMethod = null,
        string? requestPath = null,
        string? userId = null,
        int? statusCode = null,
        string? additionalData = null);
}
