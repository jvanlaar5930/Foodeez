using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class SavedRecipeRepository : BaseRepository<SavedRecipe>, ISavedRecipeRepository
{
    public SavedRecipeRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<SavedRecipe>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .ThenBy(s => s.Id)
            .Include(s => s.Recipe!).ThenInclude(r => r.Ingredients).ThenInclude(i => i.FoodItem)
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task<SavedRecipe?> FindAsync(Guid userId, Guid recipeId, CancellationToken ct = default) =>
        await _dbSet.FirstOrDefaultAsync(s => s.UserId == userId && s.RecipeId == recipeId, ct);

    public async Task<IReadOnlyList<Guid>> GetSavedRecipeIdsAsync(Guid userId, CancellationToken ct = default) =>
        await _dbSet.Where(s => s.UserId == userId).Select(s => s.RecipeId).ToListAsync(ct);
}
