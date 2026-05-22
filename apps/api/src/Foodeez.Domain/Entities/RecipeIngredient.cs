using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

public class RecipeIngredient : BaseEntity
{
    public Guid RecipeId { get; set; }
    public Guid? FoodItemId { get; set; }
    public string? IngredientName { get; set; }
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public FoodItem? FoodItem { get; set; }
}
