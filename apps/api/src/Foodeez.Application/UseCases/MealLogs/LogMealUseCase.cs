using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealLogs;

public class LogMealUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public LogMealUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealLogDto> ExecuteAsync(LogMealRequest request)
    {
        var mealLog = new MealLog
        {
            UserId = request.UserId,
            LogDate = request.LogDate,
            MealType = request.MealType,
            Notes = request.Notes
        };

        foreach (var itemRequest in request.Items)
        {
            var foodItem = await _unitOfWork.FoodItems.GetByIdAsync(itemRequest.FoodItemId);
            if (foodItem == null)
                throw new KeyNotFoundException($"FoodItem with id '{itemRequest.FoodItemId}' was not found.");

            // Scale nutrition to logged quantity relative to food item's serving size
            var scaleFactor = foodItem.ServingSize > 0
                ? itemRequest.Quantity / foodItem.ServingSize
                : itemRequest.Quantity;

            var scaledNutrition = foodItem.NutritionalInfo.Scale(scaleFactor);

            var logItem = new MealLogItem
            {
                MealLogId = mealLog.Id,
                FoodItemId = foodItem.Id,
                Quantity = itemRequest.Quantity,
                Unit = itemRequest.Unit,
                NutritionalInfo = scaledNutrition,
                FoodItem = foodItem
            };

            mealLog.Items.Add(logItem);
        }

        await _unitOfWork.MealLogs.AddAsync(mealLog);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(mealLog);
    }

    private static MealLogDto MapToDto(MealLog mealLog)
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
                FoodItem = new FoodItemDto
                {
                    Id = i.FoodItem.Id,
                    Name = i.FoodItem.Name,
                    Brand = i.FoodItem.Brand,
                    ServingSize = i.FoodItem.ServingSize,
                    ServingUnit = i.FoodItem.ServingUnit,
                    Category = i.FoodItem.Category,
                    NutritionalInfo = new NutritionalInfoDto
                    {
                        Calories = i.FoodItem.NutritionalInfo.Calories,
                        Protein = i.FoodItem.NutritionalInfo.Protein,
                        Carbohydrates = i.FoodItem.NutritionalInfo.Carbohydrates,
                        Fat = i.FoodItem.NutritionalInfo.Fat,
                        Fiber = i.FoodItem.NutritionalInfo.Fiber,
                        Sugar = i.FoodItem.NutritionalInfo.Sugar,
                        Sodium = i.FoodItem.NutritionalInfo.Sodium
                    }
                },
                NutritionalInfo = new NutritionalInfoDto
                {
                    Calories = i.NutritionalInfo.Calories,
                    Protein = i.NutritionalInfo.Protein,
                    Carbohydrates = i.NutritionalInfo.Carbohydrates,
                    Fat = i.NutritionalInfo.Fat,
                    Fiber = i.NutritionalInfo.Fiber,
                    Sugar = i.NutritionalInfo.Sugar,
                    Sodium = i.NutritionalInfo.Sodium
                }
            }).ToList(),
            TotalNutrition = new NutritionalInfoDto
            {
                Calories = total.Calories,
                Protein = total.Protein,
                Carbohydrates = total.Carbohydrates,
                Fat = total.Fat,
                Fiber = total.Fiber,
                Sugar = total.Sugar,
                Sodium = total.Sodium
            }
        };
    }
}
