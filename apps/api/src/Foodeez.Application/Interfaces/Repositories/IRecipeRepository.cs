using Foodeez.Application.DTOs.Recipes;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    /// <summary>
    /// Text matches visible to this viewer. The viewer is required rather than optional
    /// because an AI-generated recipe belongs to whoever it was generated for, and every
    /// listing has to say so - a suggestion dropdown that leaks one is the same leak as a
    /// grid that does.
    /// </summary>
    Task<IReadOnlyList<Recipe>> SearchAsync(string query, Guid? viewerId);

    /// <summary>A slice of the text matches, newest-first within relevance-neutral ordering.</summary>
    Task<IReadOnlyList<Recipe>> SearchPagedAsync(string query, Guid? viewerId, int skip, int take);

    /// <summary>
    /// A slice of the library, filtered in the database rather than in the client.
    ///
    /// Replaces the previous pair of a "whole page" read and an "everything with these tags"
    /// read: the first could not filter, and the second returned every match at once for the
    /// caller to page in memory.
    /// </summary>
    Task<IReadOnlyList<Recipe>> BrowseAsync(RecipeBrowseFilter filter, int skip, int take);

    /// <summary>
    /// This person's own recipes with any of these exact names. Used to reuse rows when a
    /// generated plan names a dish they already have, in one query rather than one per meal.
    /// </summary>
    Task<IReadOnlyList<Recipe>> GetOwnedByNamesAsync(
        Guid ownerId, IEnumerable<string> names, CancellationToken ct = default);
    Task<IReadOnlyList<Recipe>> GetBySpoonacularIdsAsync(IEnumerable<int> spoonacularIds);

    /// <summary>
    /// This person's elevated version of one recipe, or null if they have never asked for
    /// one. There is at most one: asking again rewrites it rather than adding another.
    /// </summary>
    Task<Recipe?> GetEnhancementAsync(Guid originalId, Guid ownerId, CancellationToken ct = default);

    /// <summary>
    /// Swap a tracked recipe's ingredient rows for a freshly built set. Needed because
    /// entities carry a client-assigned Guid key from birth, which an already-tracked parent
    /// causes the change tracker to misread as an existing row.
    /// </summary>
    void ReplaceIngredients(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients);
}
