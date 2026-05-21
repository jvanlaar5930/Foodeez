using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.DTOs.Recipes;

public class RecipeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string? Tags { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAIGenerated { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public NutritionalInfoDto NutritionalInfoPerServing { get; set; } = new();
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
}
