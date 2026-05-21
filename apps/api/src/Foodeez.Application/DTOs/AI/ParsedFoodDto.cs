using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.DTOs.AI;

public class ParsedFoodDto
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public float ServingSize { get; set; }
    public string ServingUnit { get; set; } = string.Empty;
    public NutritionalInfoDto NutritionalInfo { get; set; } = new();

    /// <summary>Confidence level from 0.0 to 1.0 for the AI recognition result.</summary>
    public float Confidence { get; set; }
}
