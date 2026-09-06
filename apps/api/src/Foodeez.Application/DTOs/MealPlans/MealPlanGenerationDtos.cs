namespace Foodeez.Application.DTOs.MealPlans;

/// <summary>What the generator is doing right now, sent once per day as the week is written.</summary>
public class MealPlanProgressDto
{
    public DateOnly Date { get; set; }

    /// <summary>1-based, so it reads as "day 3 of 7" without the client doing arithmetic.</summary>
    public int DayNumber { get; set; }

    public int TotalDays { get; set; }

    /// <summary>
    /// planning | saved | kept | failed. "kept" means every slot that day was already filled
    /// in by hand and was left exactly as it was.
    /// </summary>
    public string Status { get; set; } = "planning";

    /// <summary>Why, when the status is one that needs explaining. Shown as-is.</summary>
    public string? Message { get; set; }
}

/// <summary>One day's saved entries, sent as soon as that day is written rather than at the end.</summary>
public class MealPlanDayDto
{
    public DateOnly Date { get; set; }
    public Guid PlanId { get; set; }
    public List<MealPlanEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// How the whole generation turned out.
///
/// This replaces a bare <see cref="MealPlanDto"/> as the result of a generation because a
/// week is no longer all-or-nothing: some days can be written, one can fail, and the person
/// who asked for it is owed both the plan and a straight answer about which dates are not in
/// it. A client that only reads <see cref="Plan"/> still behaves exactly as it did.
/// </summary>
public class MealPlanGenerationResultDto
{
    public MealPlanDto? Plan { get; set; }

    /// <summary>Dates the model could not produce anything usable for, as yyyy-MM-dd.</summary>
    public List<string> FailedDates { get; set; } = new();

    /// <summary>Dates left untouched because they were already planned, as yyyy-MM-dd.</summary>
    public List<string> KeptDates { get; set; } = new();

    /// <summary>
    /// True when generation gave up before reaching the end of the range. Repeated failures
    /// mean the provider is down rather than the day being awkward, and grinding through five
    /// more of them at a couple of minutes each helps nobody.
    /// </summary>
    public bool StoppedEarly { get; set; }

    /// <summary>A sentence summarising the above, ready to show without the client composing one.</summary>
    public string? Message { get; set; }
}
