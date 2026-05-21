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
}
