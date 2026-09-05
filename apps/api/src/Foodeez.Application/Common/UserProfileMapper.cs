using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Common;

/// <summary>
/// The one place a <see cref="UserProfile"/> becomes a <see cref="UserProfileDto"/>.
///
/// This initializer used to be written out in five use cases, and they had drifted: the
/// recommendations copy left out <see cref="UserProfile.ExcludedFoods"/>, so the prompt that
/// asks for dietary advice was the only one that never learned about the user's allergies.
/// Three others left out DarkMode and UnitSystem. Copying fifteen properties by hand is a
/// thing that goes wrong quietly, which is why it now happens once.
/// </summary>
public static class UserProfileMapper
{
    public static UserProfileDto ToDto(UserProfile profile) => new()
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

    /// <summary>
    /// The profile of a user who may not have filled one in. Null is a meaningful answer -
    /// worth telling a model about, rather than passing off as a profile full of zeroes.
    /// </summary>
    public static UserProfileDto? ToDtoOrNull(User user) =>
        user.Profile is { } profile ? ToDto(profile) : null;
}
