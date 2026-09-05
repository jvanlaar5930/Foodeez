namespace Foodeez.Application.DTOs.Admin;

/// <summary>The filters the log viewer offers, in one parameter object.</summary>
public record AppLogQuery(
    string? Level = null,
    string? Source = null,
    string? Search = null,
    DateTime? From = null,
    DateTime? To = null,
    int Limit = 100,
    int Offset = 0);

/// <summary>A log entry in full, including the stack trace and request context.</summary>
public class AppLogDetailDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? AdditionalData { get; set; }
}

/// <summary>A page of full log entries, as the raw log viewer returns them.</summary>
public class AppLogDetailPageDto
{
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public IReadOnlyList<AppLogDetailDto> Entries { get; set; } = [];
}
