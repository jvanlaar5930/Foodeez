using Foodeez.Application.DTOs.Admin;
using Foodeez.Application.UseCases.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : FoodeezController
{
    private readonly AppLogsUseCase _logs;
    private readonly AdminUsersUseCase _users;
    private readonly AdminSettingsUseCase _settings;

    public AdminController(AppLogsUseCase logs, AdminUsersUseCase users, AdminSettingsUseCase settings)
    {
        _logs = logs;
        _users = users;
        _settings = settings;
    }

    // -- Logs -----------------------------------------------------------------

    [HttpGet("logs")]
    [ProducesResponseType(typeof(AppLogPagedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await _logs.GetPageAsync(page, pageSize, level, search, ct));

    // -- Users ----------------------------------------------------------------

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers() => Ok(await _users.ListAsync());

    [HttpPut("users/{id:guid}/toggle-admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleAdmin(Guid id)
    {
        var result = await _users.ToggleAdminAsync(id, UserId);

        return result.Outcome switch
        {
            AdminUsersUseCase.ToggleOutcome.Changed => Ok(new { isAdmin = result.Value }),
            AdminUsersUseCase.ToggleOutcome.RefusedSelf => BadRequest(Failure(result.Reason!)),
            _ => NotFound()
        };
    }

    [HttpPut("users/{id:guid}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var result = await _users.ToggleActiveAsync(id, UserId);

        return result.Outcome switch
        {
            AdminUsersUseCase.ToggleOutcome.Changed => Ok(new { isActive = result.Value }),
            AdminUsersUseCase.ToggleOutcome.RefusedSelf => BadRequest(Failure(result.Reason!)),
            _ => NotFound()
        };
    }

    // -- Settings -------------------------------------------------------------

    [HttpGet("settings")]
    [ProducesResponseType(typeof(IReadOnlyList<AppSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings() => Ok(await _settings.ListAsync());

    [HttpPost("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertSettings([FromBody] UpsertSettingsRequest request)
    {
        await _settings.UpsertAsync(request);

        return NoContent();
    }
}
