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
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }
    /// <summary>False when the upstream source has no method for this recipe at all.</summary>
    public bool HasInstructions { get; set; }

    /// <summary>
    /// True when this read tried to fetch the missing method and could not - an expired
    /// upstream quota, a network fault. Distinct from HasInstructions being false, which
    /// means the source genuinely publishes no method: one is worth retrying, the other
    /// never will be, and telling a reader "no method exists" during an outage is a lie.
    /// </summary>
    public bool DetailUnavailable { get; set; }
    public Guid? CreatedByUserId { get; set; }

    /// <summary>The recipe this one elevates, or null when this is an ordinary recipe.</summary>
    public Guid? EnhancedFromRecipeId { get; set; }

    /// <summary>When the enhancement was last written. Null on an ordinary recipe.</summary>
    public DateTime? EnhancedAt { get; set; }

    /// <summary>What the model changed and why. Null on an ordinary recipe.</summary>
    public string? EnhancementNotes { get; set; }

    /// <summary>
    /// Whether this is the elevated version of another recipe, which is what the clients
    /// badge. Sent rather than left for each client to derive from the id above, so the three
    /// of them cannot disagree about what counts as enhanced.
    /// </summary>
    public bool IsEnhanced => EnhancedFromRecipeId.HasValue;

    public NutritionalInfoDto NutritionalInfoPerServing { get; set; } = new();
    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
}
