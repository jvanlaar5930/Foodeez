using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.Common;

public static class FoodItemMapper
{
    public static FoodItemDto ToDto(FoodItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Brand = item.Brand,
        ServingSize = item.ServingSize,
        ServingUnit = item.ServingUnit,
        Category = item.Category,
        NutritionalInfo = NutritionMapper.ToDto(item.NutritionalInfo)
    };

    /// <summary>Kept so the many <c>FoodItemMapper.ToDto(nutrition)</c> call sites still read
    /// naturally; <see cref="NutritionMapper"/> is where the copy actually lives.</summary>
    public static NutritionalInfoDto ToDto(NutritionalInfo info) => NutritionMapper.ToDto(info);
}
