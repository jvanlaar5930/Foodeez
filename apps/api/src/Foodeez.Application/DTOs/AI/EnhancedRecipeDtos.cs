using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;

namespace Foodeez.Application.DTOs.AI;

/// <summary>
/// One recipe handed to a model to be raised to restaurant standard, with everything the
/// rewrite has to respect: what the dish already is, and what the cook cannot eat.
/// </summary>
public class EnhanceRecipeRequest
{
    /// <summary>The recipe as it stands, ingredients and method included.</summary>
    public RecipeDto Original { get; set; } = new();

    /// <summary>Allergies, intolerances and firm dislikes. Read from the profile, never the caller.</summary>
    public List<string> ExcludedFoods { get; set; } = new();

    /// <summary>
    /// The enhancement being replaced, when this is a refresh. Shown to the model as
    /// something to move away from - a reader asking again has already seen that version and
    /// did not want it, so handing back the same dish is the one useless answer.
    /// </summary>
    public string? PreviousEnhancement { get; set; }
}

/// <summary>An elevated version of a recipe, as the model wrote it.</summary>
public class EnhancedRecipeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Instructions { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; } = 1;
    public List<string> Tags { get; set; } = new();
    public List<EnhancedIngredientDto> Ingredients { get; set; } = new();

    /// <summary>
    /// What was changed and why, one point per entry. This is the actual advice - the part a
    /// cook reads to learn why the dish is better - so it is stored with the recipe rather
    /// than shown once and forgotten.
    /// </summary>
    public List<string> ChefNotes { get; set; } = new();

    public NutritionalInfoDto NutritionalInfoPerServing { get; set; } = new();

    /// <summary>
    /// False when nothing usable came back. The caller must not save a half-written recipe
    /// over a reader's enhancement, so this is what separates "the model wrote it" from "the
    /// provider is down" rather than an empty-looking result that reads like both.
    /// </summary>
    public bool Succeeded { get; set; } = true;
}

public class EnhancedIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// <summary>Preparation or a quality note - "finely diced", "at room temperature".</summary>
    public string? Notes { get; set; }
}
