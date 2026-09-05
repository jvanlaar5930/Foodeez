using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

/// <summary>
/// Everything the reporting screens draw for one date range, computed in a single request.
///
/// The clients used to be able to answer questions like this only by asking for every meal in
/// the range and adding it up themselves, which meant shipping every logged item over the
/// wire to render a fourteen-point line.
/// </summary>
public class NutritionReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>One entry per day in the range, in order, including days with nothing logged.</summary>
    public List<NutritionReportDayDto> Days { get; set; } = [];

    /// <summary>
    /// Averaged over the days that have something logged. A day nobody logged is a gap in the
    /// record, not a day of eating nothing, and averaging it in would read as the second.
    /// </summary>
    public NutritionTotalsDto Averages { get; set; } = new();

    /// <summary>Added up over the whole range.</summary>
    public NutritionTotalsDto Totals { get; set; } = new();

    public int DaysInRange { get; set; }
    public int DaysLogged { get; set; }

    /// <summary>Logged days landing within a tenth of the calorie target, either side.</summary>
    public int DaysOnTarget { get; set; }

    public int TotalMeals { get; set; }

    public int TargetCalories { get; set; }
    public float TargetProtein { get; set; }
    public float TargetCarbs { get; set; }
    public float TargetFat { get; set; }

    /// <summary>Where the calories came from across the day, in meal-type order.</summary>
    public List<MealTypeTotalDto> ByMealType { get; set; } = [];

    /// <summary>The foods logged most often in the range, most frequent first.</summary>
    public List<LoggedFoodTotalDto> TopFoods { get; set; } = [];
}

public class NutritionReportDayDto
{
    public DateOnly Date { get; set; }

    /// <summary>False for a day with no meals at all, which the charts draw as a gap.</summary>
    public bool HasLogs { get; set; }

    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public int MealCount { get; set; }
}

public class NutritionTotalsDto
{
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
}

public class MealTypeTotalDto
{
    public MealType MealType { get; set; }
    public float Calories { get; set; }
    public int MealCount { get; set; }
}

public class LoggedFoodTotalDto
{
    public string Name { get; set; } = string.Empty;
    public int TimesLogged { get; set; }
    public float Calories { get; set; }
}
