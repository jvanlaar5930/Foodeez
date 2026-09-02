using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.UseCases.Users;

public class GetUserProfileUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserProfileUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileDto> ExecuteAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        var profile = user.Profile;
        if (profile == null)
            throw new KeyNotFoundException($"Profile for user '{userId}' was not found.");

        return new UserProfileDto
        {
            UserId = profile.UserId,
            HeightCm = profile.HeightCm,
            WeightKg = profile.WeightKg,
            TargetWeightKg = profile.TargetWeightKg,
            Age = profile.Age,
            Gender = profile.Gender,
            ActivityLevel = profile.ActivityLevel,
            DietaryGoal = profile.DietaryGoal,
            DailyCalorieTarget = profile.DailyCalorieTarget,
            DailyProteinTargetG = profile.DailyProteinTargetG,
            DailyCarbTargetG = profile.DailyCarbTargetG,
            DailyFatTargetG = profile.DailyFatTargetG,
            Notes = profile.Notes,
            ExcludedFoods = profile.ExcludedFoods.ToList(),
            ProfileCompleted = profile.ProfileCompleted,
            DarkMode = profile.DarkMode,
            UnitSystem = profile.UnitSystem
        };
    }
}
