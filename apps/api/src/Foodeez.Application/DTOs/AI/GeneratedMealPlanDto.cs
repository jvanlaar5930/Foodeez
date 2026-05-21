using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.AI;

public class GeneratedMealPlanDto
{
    public List<GeneratedDayDto> Days { get; set; } = new();
}

public class GeneratedDayDto
{
    public DateOnly Date { get; set; }
    public List<GeneratedMealEntryDto> Meals { get; set; } = new();
}

public class GeneratedMealEntryDto
{
    public MealType MealType { get; set; }
    public string RecipeName { get; set; } = string.Empty;
    public string? RecipeDescription { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; }
    public float EstimatedCalories { get; set; }
    public float EstimatedProteinG { get; set; }
    public float EstimatedCarbsG { get; set; }
    public float EstimatedFatG { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<GeneratedIngredientDto> Ingredients { get; set; } = new();
}

public class GeneratedIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
