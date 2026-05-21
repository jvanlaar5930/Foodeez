using System.ComponentModel.DataAnnotations;

namespace Foodeez.Application.DTOs.MealPlans;

public class GenerateMealPlanRequest
{
    [Required]
    public Guid UserId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public List<string> PreferenceTags { get; set; } = new();
    public List<string> ExcludeIngredients { get; set; } = new();
}
