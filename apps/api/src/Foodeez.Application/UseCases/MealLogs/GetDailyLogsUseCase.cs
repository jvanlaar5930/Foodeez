using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealLogs;

public class GetDailyLogsUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDailyLogsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MealLogDto>> ExecuteAsync(Guid userId, DateOnly date)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        return logs.Select(MapToDto).ToList();
    }

    internal static MealLogDto MapToDto(MealLog mealLog)
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
