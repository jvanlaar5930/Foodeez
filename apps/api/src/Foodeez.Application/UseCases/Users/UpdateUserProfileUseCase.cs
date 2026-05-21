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

        profile.CalculateAndSetTargets();

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

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
            ProfileCompleted = profile.ProfileCompleted
        };
    }
}
