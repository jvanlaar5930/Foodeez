using System.ComponentModel.DataAnnotations;

namespace Foodeez.Application.DTOs.MealPlans;

public class CreateMealPlanRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
