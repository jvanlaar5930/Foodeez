using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.DTOs.AI;

/// <summary>Everything the model needs to judge a day of eating: what was logged, and what it was aiming at.</summary>
public class DayAnalysisRequest
{
    public DateOnly Date { get; set; }

    /// <summary>True while the day is still running, so recommendations can be about meals still to come.</summary>
    public bool IsToday { get; set; }

    /// <summary>The user's goal in words - "WeightLoss", "MuscleGain" - or null when no profile is set up.</summary>
    public string? DietaryGoal { get; set; }

    public NutritionSummaryDto Summary { get; set; } = new();

    public List<DayAnalysisMealRequest> Meals { get; set; } = new();
}

public class DayAnalysisMealRequest
{
    public string MealType { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
}

/// <summary>The model's read on a day: where it stands, what it is short of, and what to eat next.</summary>
public class DayAnalysisDto
{
    public int Score { get; set; }

    /// <summary>A short assessment of the day so far.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>What the day is short of - macros, food groups, micronutrients.</summary>
    public List<string> Gaps { get; set; } = new();

    /// <summary>Concrete things to eat that would round the day out.</summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>When the model produced this analysis; null for one that has never been stored.</summary>
    public DateTime? GeneratedAt { get; set; }
}
