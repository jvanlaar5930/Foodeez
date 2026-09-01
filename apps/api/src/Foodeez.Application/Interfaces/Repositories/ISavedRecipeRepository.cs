using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface ISavedRecipeRepository : IBaseRepository<SavedRecipe>
{
    /// <summary>A user's saved recipes, newest save first, with the recipe graph attached.</summary>
    Task<IReadOnlyList<SavedRecipe>> GetForUserAsync(Guid userId, CancellationToken ct = default);

    Task<SavedRecipe?> FindAsync(Guid userId, Guid recipeId, CancellationToken ct = default);

    /// <summary>Just the recipe ids, for clients that only need to light up a save button.</summary>
    Task<IReadOnlyList<Guid>> GetSavedRecipeIdsAsync(Guid userId, CancellationToken ct = default);
}
