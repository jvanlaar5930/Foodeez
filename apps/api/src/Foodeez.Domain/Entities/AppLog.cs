namespace Foodeez.Domain.Entities;

public class AppLog
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = string.Empty;       // Debug | Info | Warning | Error
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }                     // class / use-case name
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public string? RequestMethod { get; set; }
    public string? RequestPath { get; set; }
    public int? StatusCode { get; set; }
    public string? UserId { get; set; }
    public string? AdditionalData { get; set; }             // arbitrary JSON
}
