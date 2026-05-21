namespace Foodeez.Application.DTOs.MealLogs;

public class NutritionSummaryDto
{
    public DateOnly Date { get; set; }
    public float TotalCalories { get; set; }
    public float TotalProtein { get; set; }
    public float TotalCarbs { get; set; }
    public float TotalFat { get; set; }
    public float TotalFiber { get; set; }

    public int TargetCalories { get; set; }
    public float TargetProtein { get; set; }
    public float TargetCarbs { get; set; }
    public float TargetFat { get; set; }

    public float CaloriesPercentage => TargetCalories > 0 ? MathF.Round(TotalCalories / TargetCalories * 100f, 1) : 0f;
    public float ProteinPercentage => TargetProtein > 0 ? MathF.Round(TotalProtein / TargetProtein * 100f, 1) : 0f;
    public float CarbsPercentage => TargetCarbs > 0 ? MathF.Round(TotalCarbs / TargetCarbs * 100f, 1) : 0f;
    public float FatPercentage => TargetFat > 0 ? MathF.Round(TotalFat / TargetFat * 100f, 1) : 0f;
}
