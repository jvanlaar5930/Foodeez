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

    /// <summary>
    /// The recipe this one is an elevated rewrite of, or null for an ordinary recipe.
    ///
    /// An enhancement is a second row rather than an edit of the first: the original is a
    /// library recipe other people are reading, and it has to stay exactly as it is. There is
    /// at most one enhancement per recipe per person - asking again rewrites this row, so a
    /// reader has two versions to choose between and never a trail of near-identical copies.
    /// </summary>
    public Guid? EnhancedFromRecipeId { get; set; }

    /// <summary>When this enhancement was last written, so a refreshed one reads as new.</summary>
    public DateTime? EnhancedAt { get; set; }

    /// <summary>
    /// What the model changed and why, in its own words - the part of an enhancement worth
    /// reading before cooking it. Null on any recipe that is not an enhancement.
    /// </summary>
    public string? EnhancementNotes { get; set; }

    public NutritionalInfo NutritionalInfoPerServing { get; set; } = NutritionalInfo.Empty;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
