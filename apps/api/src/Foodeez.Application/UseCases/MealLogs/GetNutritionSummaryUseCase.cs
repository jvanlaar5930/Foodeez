using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.UseCases.MealLogs;

public class GetNutritionSummaryUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNutritionSummaryUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NutritionSummaryDto> ExecuteAsync(Guid userId, DateOnly date)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        var totalCalories = 0f;
        var totalProtein = 0f;
        var totalCarbs = 0f;
        var totalFat = 0f;
        var totalFiber = 0f;

        foreach (var log in logs)
        {
            var total = log.TotalNutrition;
            totalCalories += total.Calories;
            totalProtein += total.Protein;
            totalCarbs += total.Carbohydrates;
            totalFat += total.Fat;
            totalFiber += total.Fiber;
        }

        var profile = user?.Profile;

        return new NutritionSummaryDto
        {
            Date = date,
            TotalCalories = MathF.Round(totalCalories, 1),
            TotalProtein = MathF.Round(totalProtein, 1),
            TotalCarbs = MathF.Round(totalCarbs, 1),
            TotalFat = MathF.Round(totalFat, 1),
            TotalFiber = MathF.Round(totalFiber, 1),
            TargetCalories = profile?.DailyCalorieTarget ?? 0,
            TargetProtein = profile?.DailyProteinTargetG ?? 0f,
            TargetCarbs = profile?.DailyCarbTargetG ?? 0f,
            TargetFat = profile?.DailyFatTargetG ?? 0f
        };
    }
}
