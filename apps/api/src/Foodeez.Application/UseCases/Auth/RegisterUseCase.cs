using BCrypt.Net;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Auth;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.UseCases.Auth;

public class RegisterUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> ExecuteAsync(RegisterRequest request, IJwtService jwt)
    {
        var existing = await _unitOfWork.Users.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException($"A user with email '{request.Email}' already exists.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.FirstName, request.LastName);

        var profile = new UserProfile
        {
            UserId = user.Id,
            ProfileCompleted = false,
            ActivityLevel = ActivityLevel.Sedentary,
            DietaryGoal = DietaryGoal.GeneralHealth,
            Gender = Gender.PreferNotToSay
        };
        user.Profile = profile;

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

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
                ProfileCompleted = false
            }
        };
    }
}
