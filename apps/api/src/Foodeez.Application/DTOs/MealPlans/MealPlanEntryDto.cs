using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealPlans;

public class MealPlanEntryDto
{
    public Guid Id { get; set; }
    public Guid MealPlanId { get; set; }
    public DateOnly EntryDate { get; set; }
    public MealType MealType { get; set; }
    public Guid? RecipeId { get; set; }
    public string? RecipeName { get; set; }
    public Guid? FoodItemId { get; set; }
    public string? FoodItemName { get; set; }
    public string? Notes { get; set; }
    public float Servings { get; set; }
}
