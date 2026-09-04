using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealLogs;

internal static class MealLogMapper
{
    public static MealLogDto ToDto(MealLog mealLog)
    {
        var total = mealLog.TotalNutrition;

        return new MealLogDto
        {
            Id = mealLog.Id,
            LogDate = mealLog.LogDate,
            MealType = mealLog.MealType,
            Notes = mealLog.Notes,
            Items = mealLog.Items.Select(i => new MealLogItemDto
            {
                Id = i.Id,
                Quantity = i.Quantity,
                Unit = i.Unit,
                FoodItem = FoodItemMapper.ToDto(i.FoodItem),
                NutritionalInfo = NutritionMapper.ToDto(i.NutritionalInfo)
            }).ToList(),
            TotalNutrition = NutritionMapper.ToDto(total),
            Analysis = mealLog.Analysis == null ? null : MealAnalysisMapper.ToDto(mealLog.Analysis)
        };
    }
}
