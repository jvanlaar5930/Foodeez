using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class MealTemplateRepository : BaseRepository<MealTemplate>, IMealTemplateRepository
{
    public MealTemplateRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MealTemplate>> GetByUserAsync(Guid userId)
    {
        return await WithItems()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.LastUsedAt)
            .ThenByDescending(t => t.TimesUsed)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<MealTemplate?> GetDetailedByIdAsync(Guid id)
    {
        return await WithItems().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<MealTemplate?> FindByNameAsync(Guid userId, string name)
    {
        var wanted = name.Trim();
        return await WithItems()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Name == wanted);
    }

    public async Task AddItemAsync(MealTemplateItem item)
    {
        await _context.Set<MealTemplateItem>().AddAsync(item);
    }

    public void RemoveItem(MealTemplateItem item)
    {
        _context.Set<MealTemplateItem>().Remove(item);
    }

    private IQueryable<MealTemplate> WithItems() =>
        _dbSet.Include(t => t.Items).ThenInclude(i => i.FoodItem);
}
