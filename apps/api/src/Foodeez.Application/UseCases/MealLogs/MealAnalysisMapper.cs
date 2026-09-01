using Foodeez.Application.DTOs.AI;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.MealLogs;

internal static class MealAnalysisMapper
{
    public static MealAnalysisDto ToDto(MealAnalysis analysis) => new()
    {
        Score = analysis.Score,
        Completeness = analysis.Completeness,
        Missing = analysis.Missing.ToList(),
        Suggestions = analysis.Suggestions.ToList(),
        GeneratedAt = analysis.GeneratedAt
    };

    /// <summary>Describes a stored meal to the model the same way the client describes an unsaved one.</summary>
    public static MealAnalysisRequest ToRequest(MealLog mealLog) => new()
    {
        MealType = mealLog.MealType.ToString(),
        Items = mealLog.Items.Select(i => new MealAnalysisItemRequest
        {
            Name = i.FoodItem.Name,
            Amount = i.Quantity,
            Unit = i.Unit,
            Calories = i.NutritionalInfo.Calories,
            Protein = i.NutritionalInfo.Protein,
            Carbs = i.NutritionalInfo.Carbohydrates,
            Fat = i.NutritionalInfo.Fat,
            Fiber = i.NutritionalInfo.Fiber
        }).ToList()
    };
}
