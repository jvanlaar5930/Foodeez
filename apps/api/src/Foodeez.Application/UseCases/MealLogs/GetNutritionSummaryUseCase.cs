using Foodeez.Domain.ValueObjects;
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

        // The value object knows how to add itself up. The hand-rolled loop this replaces
        // accumulated five of the seven nutrients, so adding one to the summary meant
        // remembering to add it here too.
        var total = logs.Aggregate(NutritionalInfo.Empty, (running, log) => running + log.TotalNutrition);

        var profile = user?.Profile;

        return new NutritionSummaryDto
        {
            Date = date,
            TotalCalories = MathF.Round(total.Calories, 1),
            TotalProtein = MathF.Round(total.Protein, 1),
            TotalCarbs = MathF.Round(total.Carbohydrates, 1),
            TotalFat = MathF.Round(total.Fat, 1),
            TotalFiber = MathF.Round(total.Fiber, 1),
            TargetCalories = profile?.DailyCalorieTarget ?? 0,
            TargetProtein = profile?.DailyProteinTargetG ?? 0f,
            TargetCarbs = profile?.DailyCarbTargetG ?? 0f,
            TargetFat = profile?.DailyFatTargetG ?? 0f
        };
    }
}
