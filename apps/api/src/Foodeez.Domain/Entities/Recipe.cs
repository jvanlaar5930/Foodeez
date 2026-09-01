using Foodeez.Domain.Common;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class Recipe : BaseEntity
{
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
    public int? SpoonacularId { get; set; }
    public DateTime? SpoonacularSyncedAt { get; set; }

    /// <summary>Original publisher of the recipe. Some sources give us ingredients but no
    /// method, in which case this link is the only way a reader can actually cook it - and
    /// it is the attribution the upstream API expects us to display either way.</summary>
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }

    /// <summary>When the per-recipe detail endpoint was last read. Search responses carry no
    /// method at all, and for some recipes the upstream has none either - without this marker
    /// those would spend a fresh API call on every single view, forever.</summary>
    public DateTime? DetailFetchedAt { get; set; }

    public NutritionalInfo NutritionalInfoPerServing { get; set; } = NutritionalInfo.Empty;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
