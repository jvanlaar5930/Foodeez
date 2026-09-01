using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Recipes;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// Reads and edits a user's saved-recipe collection.
/// </summary>
public class SavedRecipesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public SavedRecipesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>The user's saved recipes, most recently saved first.</summary>
    public async Task<List<RecipeDto>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var saved = await _unitOfWork.SavedRecipes.GetForUserAsync(userId, ct);

        return saved
            // A recipe deleted out from under a bookmark leaves the row with nothing to show.
            // Skip it rather than handing the client a hole in the list.
            .Where(s => s.Recipe is not null)
            .Select(s => SpoonacularRecipeMapper.MapToDto(s.Recipe!))
            .ToList();
    }

    /// <summary>
    /// Saves a recipe. Idempotent: saving something already saved succeeds and changes
    /// nothing, so a client that lost track of its own state cannot produce an error.
    /// </summary>
    public async Task<bool> SaveAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        if (await _unitOfWork.Recipes.GetByIdAsync(recipeId) is null)
            return false;

        if (await _unitOfWork.SavedRecipes.FindAsync(userId, recipeId, ct) is not null)
            return true;

        await _unitOfWork.SavedRecipes.AddAsync(new Domain.Entities.SavedRecipe
        {
            UserId = userId,
            RecipeId = recipeId,
            SavedAt = DateTime.UtcNow,
        });
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Removes a save. Also idempotent - unsaving what was never saved is fine.</summary>
    public async Task UnsaveAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.SavedRecipes.FindAsync(userId, recipeId, ct);
        if (existing is null) return;

        _unitOfWork.SavedRecipes.Delete(existing);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
