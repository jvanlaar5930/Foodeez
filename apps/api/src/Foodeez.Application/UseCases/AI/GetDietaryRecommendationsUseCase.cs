using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.UseCases.AI;

public class GetDietaryRecommendationsUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;

    public GetDietaryRecommendationsUseCase(IUnitOfWork unitOfWork, IAIService aiService)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
    }

    public async Task<DietaryRecommendationsDto> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        if (user.Profile == null)
            throw new InvalidOperationException("User profile must be completed to get dietary recommendations.");

        var profileDto = UserProfileMapper.ToDto(user.Profile);

        // Aggregate last 7 days of nutrition
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sevenDaysAgo = today.AddDays(-7);
        var recentLogs = await _unitOfWork.MealLogs.GetByUserAndDateRangeAsync(userId, sevenDaysAgo, today);

        DailyNutritionDto? recentNutrition = null;
        if (recentLogs.Any())
        {
            var totalCalories = recentLogs.Sum(l => l.TotalNutrition.Calories);
            var totalProtein = recentLogs.Sum(l => l.TotalNutrition.Protein);
            var totalCarbs = recentLogs.Sum(l => l.TotalNutrition.Carbohydrates);
            var totalFat = recentLogs.Sum(l => l.TotalNutrition.Fat);
            var totalFiber = recentLogs.Sum(l => l.TotalNutrition.Fiber);

            var dayCount = Math.Max(1, recentLogs.Select(l => l.LogDate).Distinct().Count());

            var breakdown = recentLogs
                .GroupBy(l => l.MealType)
                .ToDictionary(
                    g => g.Key,
                    g => new NutritionalInfoDto
                    {
                        Calories = g.Sum(l => l.TotalNutrition.Calories),
                        Protein = g.Sum(l => l.TotalNutrition.Protein),
                        Carbohydrates = g.Sum(l => l.TotalNutrition.Carbohydrates),
                        Fat = g.Sum(l => l.TotalNutrition.Fat),
                        Fiber = g.Sum(l => l.TotalNutrition.Fiber),
                        Sugar = g.Sum(l => l.TotalNutrition.Sugar),
                        Sodium = g.Sum(l => l.TotalNutrition.Sodium)
                    }
                );

            recentNutrition = new DailyNutritionDto
            {
                Date = today,
                TotalCalories = MathF.Round(totalCalories / dayCount, 1),
                TotalProtein = MathF.Round(totalProtein / dayCount, 1),
                TotalCarbs = MathF.Round(totalCarbs / dayCount, 1),
                TotalFat = MathF.Round(totalFat / dayCount, 1),
                TotalFiber = MathF.Round(totalFiber / dayCount, 1),
                TargetCalories = user.Profile.DailyCalorieTarget,
                TargetProtein = user.Profile.DailyProteinTargetG,
                TargetCarbs = user.Profile.DailyCarbTargetG,
                TargetFat = user.Profile.DailyFatTargetG,
                MealBreakdown = breakdown
            };
        }

        return await _aiService.GetDietaryRecommendationsAsync(profileDto, recentNutrition, ct);
    }
}
