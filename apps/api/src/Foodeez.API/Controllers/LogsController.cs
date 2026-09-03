using Foodeez.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize(Roles = "Admin")]
public class LogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public LogsController(AppDbContext db) => _db = db;

    /// <summary>Query recent application logs. Intended for local development / debugging.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? level       = null,
        [FromQuery] string? source      = null,
        [FromQuery] DateTime? from      = null,
        [FromQuery] DateTime? to        = null,
        [FromQuery] int limit           = 100,
        [FromQuery] int offset          = 0)
    {
        limit = Math.Clamp(limit, 1, 500);

        var query = _db.AppLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(l => l.Level == level);

        if (!string.IsNullOrWhiteSpace(source))
            query = query.Where(l => l.Source != null && l.Source.Contains(source));

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value.ToUniversalTime());

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value.ToUniversalTime());

        var total = await query.CountAsync();

        var entries = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip(offset)
            .Take(limit)
            .Select(l => new
            {
                l.Id,
                l.Timestamp,
                l.Level,
                l.Message,
                l.Source,
                l.ExceptionType,
                l.ExceptionMessage,
                l.StackTrace,
                l.RequestMethod,
                l.RequestPath,
                l.StatusCode,
                l.UserId,
                l.AdditionalData,
            })
            .ToListAsync();

        return Ok(new { total, offset, limit, entries });
    }

    /// <summary>Get a single log entry by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLog(long id)
    {
        var entry = await _db.AppLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        return entry is null ? NotFound() : Ok(entry);
    }

    /// <summary>Delete all logs older than the given number of days (default 30).</summary>
    [HttpDelete("prune")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Prune([FromQuery] int olderThanDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var deleted = await _db.AppLogs
            .Where(l => l.Timestamp < cutoff)
            .ExecuteDeleteAsync();

        return Ok(new { deleted });
    }
}
