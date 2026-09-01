using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class RecipeRepository : BaseRepository<Recipe>, IRecipeRepository
{
    public RecipeRepository(AppDbContext context) : base(context) { }

    public override async Task<Recipe?> GetByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IReadOnlyList<Recipe>> SearchAsync(string query)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _dbSet
            .Where(r => r.Name.ToLower().Contains(lowerQuery) ||
                        (r.Description != null && r.Description.ToLower().Contains(lowerQuery)))
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> SearchPagedAsync(string query, int skip, int take)
    {
        var lowerQuery = query.ToLowerInvariant();
        return await _dbSet
            .Where(r => r.Name.ToLower().Contains(lowerQuery) ||
                        (r.Description != null && r.Description.ToLower().Contains(lowerQuery)))
            // A stable sort is what makes paging safe: without it the database is free to
            // return rows in a different order per page and items duplicate or vanish.
            .OrderBy(r => r.Name).ThenBy(r => r.Id)
            .Skip(skip)
            .Take(take)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> GetPagedAsync(int skip, int take)
    {
        return await _dbSet
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip(skip)
            .Take(take)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> GetByTagsAsync(IEnumerable<string> tags)
    {
        var tagList = tags.ToList();
        if (!tagList.Any())
            return await _dbSet.Include(r => r.Ingredients).ThenInclude(i => i.FoodItem).ToListAsync();

        var query = _dbSet.AsQueryable();
        foreach (var tag in tagList)
        {
            var lowerTag = tag.ToLowerInvariant();
            query = query.Where(r => r.Tags != null && r.Tags.ToLower().Contains(lowerTag));
        }

        return await query
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public void ReplaceIngredients(Recipe recipe, IReadOnlyList<RecipeIngredient> ingredients)
    {
        // BaseEntity assigns a Guid in its initialiser, so a freshly built ingredient already
        // has a primary key. Handed to an already-tracked parent, the change tracker reads
        // that key as an existing row and emits `UPDATE ... WHERE Id = <never inserted>`,
        // which matches nothing and throws DbUpdateConcurrencyException. Setting the state
        // explicitly is what forces the INSERT.
        if (recipe.Ingredients.Count > 0)
        {
            _context.Set<RecipeIngredient>().RemoveRange(recipe.Ingredients);
            recipe.Ingredients.Clear();
        }

        foreach (var ingredient in ingredients)
        {
            ingredient.RecipeId = recipe.Id;
            recipe.Ingredients.Add(ingredient);
            _context.Entry(ingredient).State = EntityState.Added;
        }
    }

    public async Task<IReadOnlyList<Recipe>> GetBySpoonacularIdsAsync(IEnumerable<int> spoonacularIds)
    {
        var ids = spoonacularIds.ToList();
        return await _dbSet
            .Where(r => r.SpoonacularId != null && ids.Contains(r.SpoonacularId.Value))
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }
}
