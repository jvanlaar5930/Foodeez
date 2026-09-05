using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.UseCases.Users;

public class UpdateUserProfileUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserProfileUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto> ExecuteAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        var profile = user.Profile;
        if (profile == null)
            throw new KeyNotFoundException($"Profile for user '{userId}' was not found.");

        profile.HeightCm = request.HeightCm;
        profile.WeightKg = request.WeightKg;
        profile.TargetWeightKg = request.TargetWeightKg;
        profile.Age = request.Age;
        profile.Gender = request.Gender;
        profile.ActivityLevel = request.ActivityLevel;
        profile.DietaryGoal = request.DietaryGoal;
        profile.Notes = request.Notes;
        if (request.ExcludedFoods is { } excludedFoods)
        {
            profile.SetExcludedFoods(excludedFoods);
        }
        profile.DarkMode = request.DarkMode;
        profile.UnitSystem = request.UnitSystem;

        profile.CalculateAndSetTargets();

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return UserProfileMapper.ToDto(profile);
    }
}
