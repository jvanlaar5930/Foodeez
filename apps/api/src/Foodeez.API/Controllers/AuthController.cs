using Foodeez.Application.DTOs.Auth;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;
    private readonly IJwtService _jwtService;

    public AuthController(RegisterUseCase registerUseCase, LoginUseCase loginUseCase, IJwtService jwtService)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
        _jwtService = jwtService;
    }

    /// <summary>Register a new user account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _registerUseCase.ExecuteAsync(request, _jwtService);
        return CreatedAtAction(nameof(Register), result);
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
}
