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
        // No ToLower(): MySQL's default collation compares case-insensitively already, and
        // calling it wraps the column in a function no index can be used through.
        return await _dbSet
            .Where(r => r.Name.Contains(query) ||
                        (r.Description != null && r.Description.Contains(query)))
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> SearchPagedAsync(string query, int skip, int take)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Name.Contains(query) ||
                        (r.Description != null && r.Description.Contains(query)))
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
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt).ThenBy(r => r.Id)
            .Skip(skip)
            .Take(take)
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> GetOwnedByNamesAsync(
        Guid ownerId, IEnumerable<string> names, CancellationToken ct = default)
    {
        var wanted = names.ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        // An equality IN list, not a LIKE per name: an exact name is what the caller is
        // matching on, and this can use the index that a leading-wildcard LIKE cannot.
        // Tracked on purpose - the caller attaches these to new plan entries.
        return await _dbSet
            .Where(r => r.CreatedByUserId == ownerId && wanted.Contains(r.Name))
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Recipe>> GetByTagsAsync(IEnumerable<string> tags)
    {
        var tagList = tags.ToList();
        if (tagList.Count == 0)
        {
            // Not "every recipe": an unfiltered browse is GetPagedAsync's job, and returning
            // the whole library here - tracked, with every ingredient and food item - is what
            // this used to do when someone passed an empty tag list.
            return [];
        }

        // No ToLower(): MySQL's collation compares case-insensitively already.
        var query = _dbSet.AsNoTracking();
        foreach (var tag in tagList)
        {
            query = query.Where(r => r.Tags != null && r.Tags.Contains(tag));
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
