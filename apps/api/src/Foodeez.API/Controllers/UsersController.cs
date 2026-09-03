using Foodeez.Application.DTOs.Users;
using Foodeez.Application.UseCases.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly GetUserProfileUseCase _getProfileUseCase;
    private readonly UpdateUserProfileUseCase _updateProfileUseCase;

    public UsersController(GetUserProfileUseCase getProfileUseCase, UpdateUserProfileUseCase updateProfileUseCase)
    {
        _getProfileUseCase = getProfileUseCase;
        _updateProfileUseCase = updateProfileUseCase;
    }

    /// <summary>Get a user's nutritional profile. Only the signed-in user's own profile.</summary>
    [HttpGet("{id:guid}/profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile([FromRoute] Guid id)
    {
        if (!IsSelf(id)) return Forbid();

        var profile = await _getProfileUseCase.ExecuteAsync(id);
        return Ok(profile);
    }

    /// <summary>
    /// Update a user's nutritional profile and recalculate macro targets. Only the signed-in
    /// user's own profile.
    /// </summary>
    [HttpPut("{id:guid}/profile")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromRoute] Guid id, [FromBody] UpdateProfileRequest request)
    {
        if (!IsSelf(id)) return Forbid();

        var profile = await _updateProfileUseCase.ExecuteAsync(id, request);
        return Ok(profile);
    }

    /// <summary>
    /// The route carries the id so the URL stays addressable, but it is only honoured when it
    /// is the caller's own - otherwise any signed-in user could read or overwrite anyone's
    /// profile just by changing the number in the path.
    /// </summary>
    private bool IsSelf(Guid id) => CurrentUser.IdOf(User) == id;
}
