using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.DTOs.AI;

public class DietaryRecommendationsDto
{
    public NutritionSummaryDto? DailyGoals { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<string> Deficiencies { get; set; } = new();
    public List<string> Tips { get; set; } = new();

    /// <summary>Overall health score from 0-100.</summary>
    public int OverallScore { get; set; }
}
