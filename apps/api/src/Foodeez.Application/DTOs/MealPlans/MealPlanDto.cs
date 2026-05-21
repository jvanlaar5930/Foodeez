namespace Foodeez.Application.DTOs.MealPlans;

public class MealPlanDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsAIGenerated { get; set; }

    /// <summary>
    /// Entries grouped by date string (yyyy-MM-dd).
    /// </summary>
    public Dictionary<string, List<MealPlanEntryDto>> EntriesByDate { get; set; } = new();
}
