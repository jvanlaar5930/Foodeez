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

    /// <summary>
    /// Whether the recipe in this slot is the enhanced version of another one.
    ///
    /// Sent because the two versions share a name: a calendar showing "Roast Chicken" cannot
    /// otherwise say which of them is being cooked on Thursday, and the whole point of
    /// planning the enhanced one is that it is the one you meant.
    /// </summary>
    public bool RecipeIsEnhanced { get; set; }
    public Guid? FoodItemId { get; set; }
    public string? FoodItemName { get; set; }
    public string? Notes { get; set; }
    public float Servings { get; set; }
}
