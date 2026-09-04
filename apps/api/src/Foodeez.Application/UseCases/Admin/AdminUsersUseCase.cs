using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Admin;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Admin;

/// <summary>
/// The account roster an administrator can see and act on.
///
/// The rule that an administrator cannot demote or disable themselves lived in the controller,
/// which made it an HTTP rule rather than a rule about accounts - and the only thing standing
/// between the last administrator and a system nobody can administer.
/// </summary>
public class AdminUsersUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminUsersUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();

        return users.Select(ToDto).ToList();
    }

    /// <summary>What happened to an account, so the caller can answer with the right status.</summary>
    public enum ToggleOutcome { Changed, NotFound, RefusedSelf }

    public record ToggleResult(ToggleOutcome Outcome, bool Value = false, string? Reason = null);

    public Task<ToggleResult> ToggleAdminAsync(Guid targetId, Guid requesterId) =>
        ToggleAsync(targetId, requesterId,
            "You cannot change your own admin status.",
            user => user.IsAdmin = !user.IsAdmin,
            user => user.IsAdmin);

    public Task<ToggleResult> ToggleActiveAsync(Guid targetId, Guid requesterId) =>
        ToggleAsync(targetId, requesterId,
            "You cannot disable your own account.",
            user => user.IsActive = !user.IsActive,
            user => user.IsActive);

    private async Task<ToggleResult> ToggleAsync(
        Guid targetId, Guid requesterId, string selfReason, Action<User> apply, Func<User, bool> read)
    {
        if (targetId == requesterId)
        {
            return new ToggleResult(ToggleOutcome.RefusedSelf, Reason: selfReason);
        }

        var user = await _unitOfWork.Users.GetByIdAsync(targetId);
        if (user is null)
        {
            return new ToggleResult(ToggleOutcome.NotFound);
        }

        apply(user);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new ToggleResult(ToggleOutcome.Changed, read(user));
    }

    private static AdminUserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsAdmin = user.IsAdmin,
        IsActive = user.IsActive,
        ProfileCompleted = user.Profile?.ProfileCompleted ?? false,
        CreatedAt = user.CreatedAt
    };
}
