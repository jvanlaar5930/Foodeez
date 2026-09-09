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

    /// <summary>The way back, for the features that write nutrition rather than read it.</summary>
    public static NutritionalInfo ToDomain(NutritionalInfoDto nutrition) => new(
        nutrition.Calories,
        nutrition.Protein,
        nutrition.Carbohydrates,
        nutrition.Fat,
        nutrition.Fiber,
        nutrition.Sugar,
        nutrition.Sodium);
}
