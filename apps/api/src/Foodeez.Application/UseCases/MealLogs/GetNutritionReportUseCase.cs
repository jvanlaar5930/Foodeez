using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// A range of days added up the several ways the reporting screens ask about: per day, per
/// meal type, per food, and against the user's targets.
/// </summary>
public class GetNutritionReportUseCase
{
    /// <summary>
    /// The longest range that can be asked for at once. A year of meals is already a large
    /// read, and every chart the clients draw over it is unreadable past that anyway.
    /// </summary>
    public const int MaxDays = 366;

    /// <summary>How far either side of the calorie target still counts as hitting it.</summary>
    private const float OnTargetTolerance = 0.1f;

    private const int TopFoodsShown = 10;

    private readonly IUnitOfWork _unitOfWork;

    public GetNutritionReportUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<NutritionReportDto> ExecuteAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new InvalidOperationException("The end of the range cannot fall before its start.");

        var daysInRange = endDate.DayNumber - startDate.DayNumber + 1;
        if (daysInRange > MaxDays)
            throw new InvalidOperationException($"A report covers at most {MaxDays} days.");

        var logs = await _unitOfWork.MealLogs.GetByUserAndDateRangeAsync(userId, startDate, endDate);
        var profile = (await _unitOfWork.Users.GetByIdAsync(userId))?.Profile;

        // TotalNutrition adds a meal's items up on every read, so each meal is totalled once
        // here and that total is what the four groupings below share.
        var totalled = logs.Select(log => (Log: log, Total: log.TotalNutrition)).ToList();
        var byDate = totalled.ToLookup(entry => entry.Log.LogDate);

        var days = Enumerable.Range(0, daysInRange)
            .Select(offset => startDate.AddDays(offset))
            .Select(date => Day(date, byDate[date].ToList()))
            .ToList();

        var logged = days.Where(day => day.HasLogs).ToList();
        var targetCalories = profile?.DailyCalorieTarget ?? 0;

        return new NutritionReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            Days = days,
            Averages = Average(logged),
            Totals = Sum(logged),
            DaysInRange = daysInRange,
            DaysLogged = logged.Count,
            DaysOnTarget = targetCalories > 0
                ? logged.Count(day => Math.Abs(day.Calories - targetCalories) <= targetCalories * OnTargetTolerance)
                : 0,
            TotalMeals = totalled.Count,
            TargetCalories = targetCalories,
            TargetProtein = profile?.DailyProteinTargetG ?? 0f,
            TargetCarbs = profile?.DailyCarbTargetG ?? 0f,
            TargetFat = profile?.DailyFatTargetG ?? 0f,
            ByMealType = totalled
                .GroupBy(entry => entry.Log.MealType)
                .OrderBy(group => group.Key)
                .Select(group => new MealTypeTotalDto
                {
                    MealType = group.Key,
                    Calories = Round(group.Sum(entry => entry.Total.Calories)),
                    MealCount = group.Count()
                })
                .ToList(),
            TopFoods = TopFoods(logs)
        };
    }

    private static NutritionReportDayDto Day(DateOnly date, List<(MealLog Log, NutritionalInfo Total)> meals) => new()
    {
        Date = date,
        HasLogs = meals.Count > 0,
        Calories = Round(meals.Sum(meal => meal.Total.Calories)),
        Protein = Round(meals.Sum(meal => meal.Total.Protein)),
        Carbs = Round(meals.Sum(meal => meal.Total.Carbohydrates)),
        Fat = Round(meals.Sum(meal => meal.Total.Fat)),
        Fiber = Round(meals.Sum(meal => meal.Total.Fiber)),
        MealCount = meals.Count
    };

    private static NutritionTotalsDto Sum(IReadOnlyCollection<NutritionReportDayDto> days) => new()
    {
        Calories = Round(days.Sum(day => day.Calories)),
        Protein = Round(days.Sum(day => day.Protein)),
        Carbs = Round(days.Sum(day => day.Carbs)),
        Fat = Round(days.Sum(day => day.Fat)),
        Fiber = Round(days.Sum(day => day.Fiber))
    };

    private static NutritionTotalsDto Average(IReadOnlyCollection<NutritionReportDayDto> days)
    {
        if (days.Count == 0) return new NutritionTotalsDto();

        var total = Sum(days);

        return new NutritionTotalsDto
        {
            Calories = Round(total.Calories / days.Count),
            Protein = Round(total.Protein / days.Count),
            Carbs = Round(total.Carbs / days.Count),
            Fat = Round(total.Fat / days.Count),
            Fiber = Round(total.Fiber / days.Count)
        };
    }

    /// <summary>
    /// Grouped by name rather than by food id: the same food arrives from the database, from a
    /// barcode and from the AI parser as three rows, and a reader counting how often they eat
    /// porridge does not care which of the three it was.
    /// </summary>
    private static List<LoggedFoodTotalDto> TopFoods(IEnumerable<MealLog> logs) =>
        logs.SelectMany(log => log.Items)
            .Where(item => item.FoodItem != null)
            .GroupBy(item => item.FoodItem.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LoggedFoodTotalDto
            {
                Name = group.First().FoodItem.Name,
                TimesLogged = group.Count(),
                Calories = Round(group.Sum(item => item.NutritionalInfo.Calories))
            })
            .OrderByDescending(food => food.TimesLogged)
            .ThenByDescending(food => food.Calories)
            .Take(TopFoodsShown)
            .ToList();

    private static float Round(float value) => MathF.Round(value, 1);
}
