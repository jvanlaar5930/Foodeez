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

    public NutritionalInfo NutritionalInfoPerServing { get; set; } = NutritionalInfo.Empty;

    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
}
