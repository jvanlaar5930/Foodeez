using FluentAssertions;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Domain.Tests.Entities;

public class UserProfileTests
{
    private UserProfile CreateProfile(
        Gender gender = Gender.Male,
        float heightCm = 175f,
        float weightKg = 80f,
        int age = 30,
        ActivityLevel activityLevel = ActivityLevel.Sedentary,
        DietaryGoal dietaryGoal = DietaryGoal.WeightMaintenance)
    {
        return new UserProfile
        {
            Gender = gender,
            HeightCm = heightCm,
            WeightKg = weightKg,
            Age = age,
            ActivityLevel = activityLevel,
            DietaryGoal = dietaryGoal
        };
    }

    [Fact]
    public void CalculateAndSetTargets_MaleBMR_IsHigherThanFemale()
    {
        // Arrange
        var male = CreateProfile(Gender.Male, heightCm: 175, weightKg: 80, age: 30,
            activityLevel: ActivityLevel.Sedentary, dietaryGoal: DietaryGoal.WeightMaintenance);
        var female = CreateProfile(Gender.Female, heightCm: 175, weightKg: 80, age: 30,
            activityLevel: ActivityLevel.Sedentary, dietaryGoal: DietaryGoal.WeightMaintenance);

        // Act
        male.CalculateAndSetTargets();
        female.CalculateAndSetTargets();

        // Assert — male formula adds 5, female subtracts 161
        male.DailyCalorieTarget.Should().BeGreaterThan(female.DailyCalorieTarget);
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 1.2)]
    [InlineData(ActivityLevel.LightlyActive, 1.375)]
    [InlineData(ActivityLevel.ModeratelyActive, 1.55)]
    [InlineData(ActivityLevel.VeryActive, 1.725)]
    [InlineData(ActivityLevel.ExtraActive, 1.9)]
    public void CalculateAndSetTargets_ActivityLevelMultipliers_ProduceCorrectTDEE(
        ActivityLevel activityLevel, double multiplier)
    {
        // Arrange
        var profile = CreateProfile(activityLevel: activityLevel,
            dietaryGoal: DietaryGoal.WeightMaintenance);

        // Male BMR for 80kg, 175cm, 30yo (Mifflin-St Jeor):
        // (10 * 80) + (6.25 * 175) - (5 * 30) + 5 = 800 + 1093.75 - 150 + 5 = 1748.75
        double bmr = (10.0 * 80) + (6.25 * 175) - (5.0 * 30) + 5;
        var expectedCalories = (int)Math.Round(bmr * multiplier);

        // Act
        profile.CalculateAndSetTargets();

        // Assert — allow ±15 for floating-point rounding
        profile.DailyCalorieTarget.Should().BeCloseTo(expectedCalories, 15);
    }

    [Fact]
    public void CalculateAndSetTargets_WeightLoss_CreatesCalorieDeficit()
    {
        // Arrange
        var maintenance = CreateProfile(dietaryGoal: DietaryGoal.WeightMaintenance);
        var weightLoss = CreateProfile(dietaryGoal: DietaryGoal.WeightLoss);

        // Act
        maintenance.CalculateAndSetTargets();
        weightLoss.CalculateAndSetTargets();

        // Assert — weight loss should have 500 fewer calories
        (maintenance.DailyCalorieTarget - weightLoss.DailyCalorieTarget).Should().BeCloseTo(500, 5);
    }

    [Fact]
    public void CalculateAndSetTargets_MuscleGain_CreatesSurplus()
    {
        // Arrange
        var maintenance = CreateProfile(dietaryGoal: DietaryGoal.WeightMaintenance);
        var muscleGain = CreateProfile(dietaryGoal: DietaryGoal.MuscleGain);

        // Act
        maintenance.CalculateAndSetTargets();
        muscleGain.CalculateAndSetTargets();

        // Assert — muscle gain has surplus
        muscleGain.DailyCalorieTarget.Should().BeGreaterThan(maintenance.DailyCalorieTarget);
        (muscleGain.DailyCalorieTarget - maintenance.DailyCalorieTarget).Should().BeCloseTo(300, 5);
    }

    [Fact]
    public void CalculateAndSetTargets_MacroTargets_SumToApproximatelyTotalCalories()
    {
        // Arrange
        var profile = CreateProfile(dietaryGoal: DietaryGoal.WeightMaintenance,
            activityLevel: ActivityLevel.ModeratelyActive);

        // Act
        profile.CalculateAndSetTargets();

        // Calculate approximate calories from macros
        // Protein: 4 kcal/g, Carbs: 4 kcal/g, Fat: 9 kcal/g
        var caloriesFromMacros =
            profile.DailyProteinTargetG * 4f +
            profile.DailyCarbTargetG * 4f +
            profile.DailyFatTargetG * 9f;

        // Should be within 10% of total target
        var tolerance = profile.DailyCalorieTarget * 0.10f;
        caloriesFromMacros.Should().BeApproximately(profile.DailyCalorieTarget, tolerance);
    }

    [Fact]
    public void CalculateAndSetTargets_WithZeroValues_DoesNotSetTargets()
    {
        // Arrange
        var profile = new UserProfile
        {
            Gender = Gender.Male,
            HeightCm = 0,
            WeightKg = 0,
            Age = 0,
            ActivityLevel = ActivityLevel.Sedentary,
            DietaryGoal = DietaryGoal.WeightMaintenance
        };

        // Act
        profile.CalculateAndSetTargets();

        // Assert — should not change the defaults (0)
        profile.DailyCalorieTarget.Should().Be(0);
    }

    [Fact]
    public void CalculateAndSetTargets_ProfileCompleted_SetToTrue_WhenValidData()
    {
        // Arrange
        var profile = CreateProfile();

        // Act
        profile.CalculateAndSetTargets();

        // Assert
        profile.ProfileCompleted.Should().BeTrue();
    }

    [Fact]
    public void CalculateAndSetTargets_MuscleGain_HasHigherProteinTarget()
    {
        // Arrange
        var generalHealth = CreateProfile(dietaryGoal: DietaryGoal.GeneralHealth);
        var muscleGain = CreateProfile(dietaryGoal: DietaryGoal.MuscleGain);

        // Act
        generalHealth.CalculateAndSetTargets();
        muscleGain.CalculateAndSetTargets();

        // Assert — muscle gain uses 1.2g/kg vs 0.8g/kg
        muscleGain.DailyProteinTargetG.Should().BeGreaterThan(generalHealth.DailyProteinTargetG);
    }
}
