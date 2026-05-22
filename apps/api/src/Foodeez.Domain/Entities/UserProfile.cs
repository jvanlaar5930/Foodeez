using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;

namespace Foodeez.Domain.Entities;

public class UserProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public float HeightCm { get; set; }
    public float WeightKg { get; set; }
    public float? TargetWeightKg { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public ActivityLevel ActivityLevel { get; set; }
    public DietaryGoal DietaryGoal { get; set; }
    public int DailyCalorieTarget { get; set; }
    public float DailyProteinTargetG { get; set; }
    public float DailyCarbTargetG { get; set; }
    public float DailyFatTargetG { get; set; }
    public string? Notes { get; set; }
    public bool ProfileCompleted { get; set; }
    public bool DarkMode { get; set; }

    public User User { get; set; } = null!;

    /// <summary>
    /// Calculates BMR using Mifflin-St Jeor equation, adjusts for activity level,
    /// applies goal-based calorie adjustment, and sets macro targets.
    /// </summary>
    public void CalculateAndSetTargets()
    {
        if (HeightCm <= 0 || WeightKg <= 0 || Age <= 0)
            return;

        // Mifflin-St Jeor BMR
        double bmr;
        if (Gender == Gender.Male)
        {
            bmr = (10.0 * WeightKg) + (6.25 * HeightCm) - (5.0 * Age) + 5;
        }
        else
        {
            // Female, Other, and PreferNotToSay use female formula
            bmr = (10.0 * WeightKg) + (6.25 * HeightCm) - (5.0 * Age) - 161;
        }

        // Activity level multiplier
        double activityMultiplier = ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2,
            ActivityLevel.LightlyActive => 1.375,
            ActivityLevel.ModeratelyActive => 1.55,
            ActivityLevel.VeryActive => 1.725,
            ActivityLevel.ExtraActive => 1.9,
            _ => 1.2
        };

        double tdee = bmr * activityMultiplier;

        // Goal-based adjustment
        double targetCalories = DietaryGoal switch
        {
            DietaryGoal.WeightLoss => tdee - 500,
            DietaryGoal.WeightGain => tdee + 300,
            DietaryGoal.MuscleGain => tdee + 300,
            DietaryGoal.WeightMaintenance => tdee,
            DietaryGoal.GeneralHealth => tdee,
            _ => tdee
        };

        // Ensure minimum safe calories
        targetCalories = Math.Max(targetCalories, 1200);
        DailyCalorieTarget = (int)Math.Round(targetCalories);

        // Macro targets
        // Protein: 0.8-1.2g per kg body weight (higher for muscle gain)
        float proteinPerKg = DietaryGoal == DietaryGoal.MuscleGain ? 1.2f : 0.8f;
        DailyProteinTargetG = (float)Math.Round(WeightKg * proteinPerKg, 1);

        // Protein calories (4 kcal/g)
        double proteinCalories = DailyProteinTargetG * 4.0;

        // Remaining calories for carbs and fat
        double remainingCalories = targetCalories - proteinCalories;

        // Carbs: ~55% of remaining (range 45-55%), Fat: ~35% of remaining (range 20-35%)
        double carbCalories = remainingCalories * 0.55;
        double fatCalories = remainingCalories * 0.35;

        // Carbs: 4 kcal/g, Fat: 9 kcal/g
        DailyCarbTargetG = (float)Math.Round(carbCalories / 4.0, 1);
        DailyFatTargetG = (float)Math.Round(fatCalories / 9.0, 1);

        ProfileCompleted = HeightCm > 0 && WeightKg > 0 && Age > 0;
    }
}
