using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.AI;

public class GetDietaryRecommendationsUseCase
{
    /// <summary>How far back "how they have been eating lately" reaches.</summary>
    private const int RecentDays = 7;

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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recentLogs = await _unitOfWork.MealLogs.GetByUserAndDateRangeAsync(
            userId, today.AddDays(-RecentDays), today);

        return await _aiService.GetDietaryRecommendationsAsync(
            UserProfileMapper.ToDto(user.Profile),
            RecentNutrition(recentLogs, user.Profile, today),
            ct);
    }

    /// <summary>
    /// An average day over the recent window, plus what each meal slot contributed.
    ///
    /// MealLog.TotalNutrition re-adds every item in the meal on each read, and this used to
    /// read it twelve times per log - five running totals and seven per group. Adding each
    /// log's total up once instead means one pass over the items rather than twelve.
    /// </summary>
    private static DailyNutritionDto? RecentNutrition(
        IReadOnlyCollection<MealLog> logs, UserProfile profile, DateOnly today)
    {
        if (logs.Count == 0)
        {
            return null;
        }

        var totals = logs.Select(log => (log.MealType, log.LogDate, Nutrition: log.TotalNutrition)).ToList();
        var overall = totals.Aggregate(NutritionalInfo.Empty, (running, entry) => running + entry.Nutrition);

        // Days with nothing logged are not averaged in: someone who logged two days of the
        // seven ate what they ate on those two days, and dividing by seven would describe
        // somebody starving.
        var dayCount = Math.Max(1, totals.Select(entry => entry.LogDate).Distinct().Count());

        return new DailyNutritionDto
        {
            Date = today,
            TotalCalories = MathF.Round(overall.Calories / dayCount, 1),
            TotalProtein = MathF.Round(overall.Protein / dayCount, 1),
            TotalCarbs = MathF.Round(overall.Carbohydrates / dayCount, 1),
            TotalFat = MathF.Round(overall.Fat / dayCount, 1),
            TotalFiber = MathF.Round(overall.Fiber / dayCount, 1),
            TargetCalories = profile.DailyCalorieTarget,
            TargetProtein = profile.DailyProteinTargetG,
            TargetCarbs = profile.DailyCarbTargetG,
            TargetFat = profile.DailyFatTargetG,
            MealBreakdown = totals
                .GroupBy(entry => entry.MealType)
                .ToDictionary(
                    group => group.Key,
                    group => NutritionMapper.ToDto(group.Aggregate(
                        NutritionalInfo.Empty, (running, entry) => running + entry.Nutrition)))
        };
    }
}
