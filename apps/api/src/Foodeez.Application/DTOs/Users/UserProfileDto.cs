using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.Users;

public class UserProfileDto
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
    public UnitSystem UnitSystem { get; set; }
}
