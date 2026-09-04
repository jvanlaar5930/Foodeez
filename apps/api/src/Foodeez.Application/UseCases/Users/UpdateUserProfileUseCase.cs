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
            profile.ExcludedFoods = NormalizeExclusions(excludedFoods);
        }
        profile.DarkMode = request.DarkMode;
        profile.UnitSystem = request.UnitSystem;

        profile.CalculateAndSetTargets();

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return UserProfileMapper.ToDto(profile);
    }

    // A prompt has to carry every one of these, so the list is bounded at both ends: enough
    // entries for a real set of allergies and dislikes, none of them long enough to be a
    // paragraph of injected instructions.
    private const int MaxExclusions = 50;
    private const int MaxExclusionLength = 60;

    /// <summary>
    /// Trims, drops blanks and removes case-insensitive duplicates. These are typed by hand
    /// and end up in a prompt, so "Peanuts", "peanuts " and "" must not all get there.
    /// </summary>
    private static List<string> NormalizeExclusions(IEnumerable<string> excluded) =>
        excluded
            .Select(food => food.Trim())
            .Where(food => food.Length > 0)
            .Select(food => food.Length > MaxExclusionLength ? food[..MaxExclusionLength] : food)
            .DistinctBy(food => food.ToLowerInvariant())
            .Take(MaxExclusions)
            .ToList();
}
