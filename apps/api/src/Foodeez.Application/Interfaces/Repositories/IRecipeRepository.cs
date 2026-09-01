using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    Task<IReadOnlyList<Recipe>> SearchAsync(string query);

    /// <summary>A slice of the text matches, newest-first within relevance-neutral ordering.</summary>
    Task<IReadOnlyList<Recipe>> SearchPagedAsync(string query, int skip, int take);

    /// <summary>A slice of the whole library, for browsing with no query.</summary>
    Task<IReadOnlyList<Recipe>> GetPagedAsync(int skip, int take);
    Task<IReadOnlyList<Recipe>> GetByTagsAsync(IEnumerable<string> tags);
    Task<IReadOnlyList<Recipe>> GetBySpoonacularIdsAsync(IEnumerable<int> spoonacularIds);

    /// <summary>
    /// Swap a tracked recipe's ingredient rows for a freshly built set. Needed because
    /// entities carry a client-assigned Guid key from birth, which an already-tracked parent
    /// causes the change tracker to misread as an existing row.
    /// </summary>
    void ReplaceIngredients(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients);
}
