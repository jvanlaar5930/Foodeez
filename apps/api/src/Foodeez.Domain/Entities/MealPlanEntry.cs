using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;

namespace Foodeez.Domain.Entities;

public class MealPlanEntry : BaseEntity
{
    public Guid MealPlanId { get; set; }
    public DateOnly EntryDate { get; set; }
    public MealType MealType { get; set; }
    public Guid? RecipeId { get; set; }
    public Guid? FoodItemId { get; set; }
    public string? Notes { get; set; }
    public float Servings { get; set; } = 1f;

    public MealPlan MealPlan { get; set; } = null!;
    public Recipe? Recipe { get; set; }
    public FoodItem? FoodItem { get; set; }
}
