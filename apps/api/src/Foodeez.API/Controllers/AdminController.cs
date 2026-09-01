using System.Security.Claims;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ── Logs ─────────────────────────────────────────────────────────────

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? level = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page = Math.Max(1, page);

        var (items, total) = await _unitOfWork.AppLogs.GetPagedAsync(page, pageSize, level, search, ct);

        return Ok(new AppLogPagedDto
        {
            Items = items.Select(x => new AppLogDto
            {
                Id = x.Id,
                Level = x.Level,
                Message = x.Message,
                Exception = x.ExceptionMessage,
                Source = x.Source,
                Timestamp = x.Timestamp
            }).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    // ── Users ─────────────────────────────────────────────────────────────

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var dtos = users.Select(u => new AdminUserDto
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsAdmin = u.IsAdmin,
            IsActive = u.IsActive,
            ProfileCompleted = u.Profile?.ProfileCompleted ?? false,
            CreatedAt = u.CreatedAt
        });
        return Ok(dtos);
    }

    [HttpPut("users/{id:guid}/toggle-admin")]
    public async Task<IActionResult> ToggleAdmin(Guid id)
    {
        var requesterId = GetCurrentUserId();
        if (requesterId == id)
            return BadRequest(new { detail = "You cannot change your own admin status." });

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return NotFound();

        user.IsAdmin = !user.IsAdmin;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { isAdmin = user.IsAdmin });
    }

    [HttpPut("users/{id:guid}/toggle-active")]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var requesterId = GetCurrentUserId();
        if (requesterId == id)
            return BadRequest(new { detail = "You cannot disable your own account." });

        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return NotFound();

        user.IsActive = !user.IsActive;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { isActive = user.IsActive });
    }

    // ── Settings ──────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _unitOfWork.AppSettings.GetAllAsync();
        return Ok(settings.Select(s => new AppSettingDto
        {
            Key = s.Key,
            Value = s.IsSecret ? MaskSecret(s.Value) : s.Value,
            Category = s.Category,
            Description = s.Description,
            IsSecret = s.IsSecret,
            UpdatedAt = s.UpdatedAt
        }));
    }

    [HttpPost("settings")]
    public async Task<IActionResult> UpsertSettings([FromBody] UpsertSettingsRequest request)
    {
        var pairs = request.Settings.Select(s => (s.Key, s.Value));
        await _unitOfWork.AppSettings.UpsertManyAsync(pairs);
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private static string MaskSecret(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= 8) return "****";
        return value[..4] + new string('*', value.Length - 8) + value[^4..];
    }
}
