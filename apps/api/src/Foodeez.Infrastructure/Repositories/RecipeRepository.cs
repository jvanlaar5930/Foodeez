using Foodeez.Application.DTOs.Recipes;
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

    /// <summary>
    /// Hides AI-generated recipes that belong to somebody else.
    ///
    /// They are written into the shared library as a plan is generated, which is what makes
    /// them reusable - but they were written *for* one person, from their targets and their
    /// excluded foods, and everyone else was seeing them in the same list as the real ones.
    /// A signed-out caller sees none of them at all.
    /// </summary>
    private static IQueryable<Recipe> VisibleTo(IQueryable<Recipe> query, Guid? viewerId) =>
        viewerId is { } viewer
            ? query.Where(r => !r.IsAIGenerated || r.CreatedByUserId == viewer)
            : query.Where(r => !r.IsAIGenerated);

    public async Task<IReadOnlyList<Recipe>> SearchAsync(string query, Guid? viewerId)
    {
        // No ToLower(): MySQL's default collation compares case-insensitively already, and
        // calling it wraps the column in a function no index can be used through.
        return await VisibleTo(_dbSet, viewerId)
            .Where(r => r.Name.Contains(query) ||
                        (r.Description != null && r.Description.Contains(query)))
            .Include(r => r.Ingredients)
                .ThenInclude(i => i.FoodItem)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Recipe>> SearchPagedAsync(string query, Guid? viewerId, int skip, int take)
    {
        return await VisibleTo(_dbSet.AsNoTracking(), viewerId)
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

    public async Task<IReadOnlyList<Recipe>> BrowseAsync(RecipeBrowseFilter filter, int skip, int take)
    {
        var query = VisibleTo(_dbSet.AsNoTracking(), filter.ViewerId);

        if (filter.OnlyPreviousMeals)
        {
            // Nobody's own meals when nobody is signed in, rather than everybody's.
            query = filter.ViewerId is { } viewer
                ? query.Where(r => r.IsAIGenerated && r.CreatedByUserId == viewer)
                : query.Where(_ => false);
        }

        if (filter.OnlyFavorites)
        {
            // A join row rather than a flag, because the library is shared - see SavedRecipe.
            query = filter.ViewerId is { } viewer
                ? query.Where(r => _context.SavedRecipes.Any(s => s.RecipeId == r.Id && s.UserId == viewer))
                : query.Where(_ => false);
        }

        // No ToLower(): MySQL's collation compares case-insensitively already.
        foreach (var tag in filter.Tags)
        {
            query = query.Where(r => r.Tags != null && r.Tags.Contains(tag));
        }

        return await query
            // A stable sort is what makes paging safe - without it the database is free to
            // return the same row on two consecutive pages, and skip another entirely.
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
