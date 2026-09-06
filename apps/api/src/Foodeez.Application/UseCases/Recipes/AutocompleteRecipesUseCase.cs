using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// Search-as-you-type suggestions: locally stored recipes first, topped up with
/// Spoonacular's title-only autocomplete (cheap, no nutrition/instruction payload).
/// </summary>
public class AutocompleteRecipesUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpoonacularService _spoonacular;

    private const int MaxSuggestions = 8;

    public AutocompleteRecipesUseCase(IUnitOfWork unitOfWork, ISpoonacularService spoonacular)
    {
        _unitOfWork = unitOfWork;
        _spoonacular = spoonacular;
    }

    public async Task<List<RecipeSuggestionDto>> ExecuteAsync(string query, Guid? viewerId = null, CancellationToken ct = default)
    {
        var suggestions = new List<RecipeSuggestionDto>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Shorter names rank first: they're the closest match to a partial query.
        var local = await _unitOfWork.Recipes.SearchAsync(query, viewerId);
        foreach (var recipe in local.OrderBy(r => r.Name.Length).Take(MaxSuggestions))
        {
            if (!seen.Add(recipe.Name)) continue;
            suggestions.Add(new RecipeSuggestionDto
            {
                Name = recipe.Name,
                RecipeId = recipe.Id,
                ImageUrl = recipe.ImageUrl
            });
        }

        if (suggestions.Count >= MaxSuggestions)
            return suggestions;

        var remote = await _spoonacular.AutocompleteAsync(query, MaxSuggestions, ct);
        foreach (var hit in remote)
        {
            if (suggestions.Count >= MaxSuggestions) break;
            if (!seen.Add(hit.Title)) continue;
            suggestions.Add(new RecipeSuggestionDto { Name = hit.Title });
        }

        return suggestions;
    }
}
