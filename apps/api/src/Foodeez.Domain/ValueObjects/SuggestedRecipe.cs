namespace Foodeez.Domain.ValueObjects;

/// <summary>
/// A recipe the assistant wrote out during a conversation, in the shape the recipe library
/// needs to store one.
///
/// Kept on the message rather than written straight to the library for the same reason a
/// <see cref="PlannedMeal"/> is: a model thinking out loud about three ways to cook a chicken
/// breast should not leave three rows in a catalogue everyone else searches. It becomes a
/// real recipe only when the reader keeps it.
/// </summary>
public class SuggestedRecipe
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>The method, as numbered steps in one block - the shape Recipe.Instructions holds.</summary>
    public string Instructions { get; set; } = string.Empty;

    public int PrepTimeMinutes { get; set; }
    public int CookTimeMinutes { get; set; }
    public int Servings { get; set; } = 1;

    /// <summary>Comma-joined, matching how the recipe library stores tags.</summary>
    public string? Tags { get; set; }

    public List<SuggestedRecipeIngredient> Ingredients { get; set; } = new();

    /// <summary>Per serving, and the model's estimate - flat numbers rather than a
    /// NutritionalInfo, which is immutable and has no place in a JSON column.</summary>
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}

/// <summary>One line of a suggested recipe's ingredient list.</summary>
public class SuggestedRecipeIngredient
{
    public string Name { get; set; } = string.Empty;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
