namespace Foodeez.Application.DTOs.Recipes;

public class RecipeIngredientDto
{
    public Guid FoodItemId { get; set; }
    public string FoodItemName { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
