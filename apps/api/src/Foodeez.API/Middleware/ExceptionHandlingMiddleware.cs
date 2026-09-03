using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IAppLogger _appLogger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IAppLogger appLogger)
    {
        _next      = next;
        _logger    = logger;
        _appLogger = appLogger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (statusCode, _) = Classify(ex);

            _appLogger.LogError(
                ex.Message,
                exception:     ex,
                source:        nameof(ExceptionHandlingMiddleware),
                requestMethod: context.Request.Method,
                requestPath:   context.Request.Path,
                userId:        userId,
                statusCode:    statusCode);

            await WriteResponseAsync(context, ex);
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = Classify(exception);

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = exception.Message,
            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    private static (int statusCode, string title) Classify(Exception ex) => ex switch
    {
        KeyNotFoundException       => (StatusCodes.Status404NotFound,            "Resource Not Found"),
        UnauthorizedAccessException=> (StatusCodes.Status401Unauthorized,        "Unauthorized"),
        ValidationException        => (StatusCodes.Status400BadRequest,          "Validation Error"),
        ArgumentException          => (StatusCodes.Status400BadRequest,          "Invalid Argument"),
        InvalidOperationException  => (StatusCodes.Status400BadRequest,          "Invalid Operation"),
        ConcurrencyConflictException => (StatusCodes.Status409Conflict,          "Concurrency Conflict"),
        _                          => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
    };
}
