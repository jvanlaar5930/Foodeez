using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealPlans;

/// <summary>
/// One meal in one slot. The same shape serves both adding and editing: a slot holds a
/// recipe, a food item or just a written-down meal, and all three are the same edit to
/// the user.
/// </summary>
public class MealPlanEntryRequest
{
    [Required]
    public DateOnly EntryDate { get; set; }

    [Required]
    public MealType MealType { get; set; }

    public Guid? RecipeId { get; set; }

    public Guid? FoodItemId { get; set; }

    /// <summary>
    /// Free text for a meal with nothing in the catalogue behind it. This is also where
    /// AI-generated meals keep their name, so it is a first-class way to fill a slot,
    /// not a leftover comment field.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Range(0.01, 100)]
    public float Servings { get; set; } = 1f;
}
