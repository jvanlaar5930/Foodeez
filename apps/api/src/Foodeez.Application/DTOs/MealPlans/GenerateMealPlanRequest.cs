using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Foodeez.Application.DTOs.MealPlans;

public class GenerateMealPlanRequest
{
    [Required]
    public Guid UserId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public List<string> PreferenceTags { get; set; } = new();
    public List<string> ExcludeIngredients { get; set; } = new();

    /// <summary>
    /// Free text the user typed for this one generation - "more variety in the dinners",
    /// "reuse last week's breakfasts". Steers the plan without becoming a saved preference,
    /// since a request made once is rarely meant to hold forever.
    /// </summary>
    [MaxLength(1000)]
    public string? Guidance { get; set; }

    /// <summary>
    /// What was already planned for the period before this one, as "date mealType: name"
    /// lines. Filled in by the server and never bound from the request - it exists so an
    /// instruction like "reuse last week's breakfasts" has a real week to reuse rather than
    /// one the model invents, and it is only gathered when there is guidance to act on it.
    /// </summary>
    [JsonIgnore]
    public List<string> PreviousPeriod { get; set; } = new();
}
