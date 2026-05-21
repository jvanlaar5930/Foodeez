using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

public class DailyNutritionDto : NutritionSummaryDto
{
    public Dictionary<MealType, NutritionalInfoDto> MealBreakdown { get; set; } = new();
}
