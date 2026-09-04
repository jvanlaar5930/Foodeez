using Foodeez.Application.DTOs.Admin;
using Foodeez.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

/// <summary>
/// The raw log, with stack traces and request context - what you want when something has
/// already gone wrong. The admin UI uses /api/admin/logs for its summary view; nothing in
/// either client calls these routes, so they exist to be read by hand.
/// </summary>
[Route("api/logs")]
[Authorize(Roles = "Admin")]
public class LogsController : FoodeezController
{
    private readonly AppLogsUseCase _logs;

    public LogsController(AppLogsUseCase logs) => _logs = logs;

    /// <summary>Query recent application logs.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(AppLogDetailPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? level = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken ct = default) =>
        Ok(await _logs.QueryAsync(
            new AppLogQuery(level, source, From: from, To: to, Limit: limit, Offset: offset), ct));

    /// <summary>Get a single log entry by ID.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AppLogDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLog(long id, CancellationToken ct) =>
        await _logs.GetAsync(id, ct) is { } entry ? Ok(entry) : NotFound();

    /// <summary>Delete all logs older than the given number of days (default 30).</summary>
    [HttpDelete("prune")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Prune([FromQuery] int olderThanDays = 30, CancellationToken ct = default) =>
        Ok(new { deleted = await _logs.PruneAsync(olderThanDays, ct) });
}
