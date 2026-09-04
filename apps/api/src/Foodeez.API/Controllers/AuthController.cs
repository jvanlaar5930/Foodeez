using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Auth;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[Route("api/auth")]
public class AuthController : FoodeezController
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthController(RegisterUseCase registerUseCase, LoginUseCase loginUseCase, IJwtService jwtService, IUnitOfWork unitOfWork)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _registerUseCase.ExecuteAsync(request, _jwtService);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(Failure(ex.Message, StatusCodes.Status409Conflict));
        }
    }

    /// <summary>Authenticate and receive a JWT token.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _loginUseCase.ExecuteAsync(request, _jwtService);
        return Ok(result);
    }

    /// <summary>Return the currently authenticated user (used to rehydrate session after page refresh).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var user = await _unitOfWork.Users.GetByIdAsync(UserId);
        if (user is null)
            return Unauthorized();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfileCompleted = user.Profile?.ProfileCompleted ?? false,
            IsAdmin = user.IsAdmin,
            IsActive = user.IsActive
        });
    }
}
