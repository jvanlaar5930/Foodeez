using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.Common;

/// <summary>
/// The one place a <see cref="NutritionalInfo"/> becomes a <see cref="NutritionalInfoDto"/>.
/// The seven-field copy was written out nine times; adding an eighth nutrient meant finding
/// all nine.
/// </summary>
public static class NutritionMapper
{
    public static NutritionalInfoDto ToDto(NutritionalInfo nutrition) => new()
    {
        Calories = nutrition.Calories,
        Protein = nutrition.Protein,
        Carbohydrates = nutrition.Carbohydrates,
        Fat = nutrition.Fat,
        Fiber = nutrition.Fiber,
        Sugar = nutrition.Sugar,
        Sodium = nutrition.Sodium
    };
}
