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
        NutritionalInfo = ToDto(item.NutritionalInfo)
    };

    public static NutritionalInfoDto ToDto(NutritionalInfo info) => new()
    {
        Calories = info.Calories,
        Protein = info.Protein,
        Carbohydrates = info.Carbohydrates,
        Fat = info.Fat,
        Fiber = info.Fiber,
        Sugar = info.Sugar,
        Sodium = info.Sodium
    };
}
