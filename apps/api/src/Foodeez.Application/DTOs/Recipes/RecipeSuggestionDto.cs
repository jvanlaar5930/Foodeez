namespace Foodeez.Application.DTOs.Recipes;

/// <summary>
/// A single search-as-you-type suggestion. <see cref="RecipeId"/> is set only when the
/// suggestion already exists locally and can therefore be opened directly.
/// </summary>
public class RecipeSuggestionDto
{
    public string Name { get; set; } = string.Empty;
    public Guid? RecipeId { get; set; }
    public string? ImageUrl { get; set; }
}
