using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Auth;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.Auth;

public class LoginUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public LoginUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> ExecuteAsync(LoginRequest request, IJwtService jwt)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var token = jwt.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ProfileCompleted = user.Profile?.ProfileCompleted ?? false
            }
        };
    }
}
