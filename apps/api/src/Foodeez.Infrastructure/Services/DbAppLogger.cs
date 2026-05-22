using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public sealed class DbAppLogger : IAppLogger
{
    private readonly LogQueue _queue;
    private readonly ILogger<DbAppLogger> _fallback;

    public DbAppLogger(LogQueue queue, ILogger<DbAppLogger> fallback)
    {
        _queue = queue;
        _fallback = fallback;
    }

    public void LogDebug(string message, string? source = null) =>
        Enqueue("Debug", message, source: source);

    public void LogInfo(string message, string? source = null, string? userId = null) =>
        Enqueue("Info", message, source: source, userId: userId);

    public void LogWarning(string message, string? source = null, string? userId = null) =>
        Enqueue("Warning", message, source: source, userId: userId);

    public void LogError(
        string message,
        Exception? exception = null,
        string? source = null,
        string? requestMethod = null,
        string? requestPath = null,
        string? userId = null,
        int? statusCode = null,
        string? additionalData = null) =>
        Enqueue("Error", message, exception, source, requestMethod, requestPath, userId, statusCode, additionalData);

    private void Enqueue(
        string level,
        string message,
        Exception? exception = null,
        string? source = null,
        string? requestMethod = null,
        string? requestPath = null,
        string? userId = null,
        int? statusCode = null,
        string? additionalData = null)
    {
        var entry = new AppLog
        {
            Timestamp     = DateTime.UtcNow,
            Level         = level,
            Message       = message,
            Source        = source,
            ExceptionType = exception?.GetType().FullName,
            ExceptionMessage = exception?.Message,
            StackTrace    = exception?.StackTrace,
            RequestMethod = requestMethod,
            RequestPath   = requestPath,
            UserId        = userId,
            StatusCode    = statusCode,
            AdditionalData = additionalData,
        };

        if (!_queue.Writer.TryWrite(entry))
            _fallback.LogError("[LogQueue full] {Level} {Message}", level, message);
    }
}
