using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class MealPlanRepository : BaseRepository<MealPlan>, IMealPlanRepository
{
    public MealPlanRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MealPlan>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .Include(p => p.Entries)
                .ThenInclude(e => e.Recipe)
            .Include(p => p.Entries)
                .ThenInclude(e => e.FoodItem)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();
    }

    public async Task AddEntryAsync(MealPlanEntry entry)
    {
        await _context.Set<MealPlanEntry>().AddAsync(entry);
    }

    public void RemoveEntry(MealPlanEntry entry)
    {
        _context.Set<MealPlanEntry>().Remove(entry);
    }

    public async Task<MealPlan?> GetWithEntriesAsync(Guid planId)
    {
        return await _dbSet
            .Where(p => p.Id == planId)
            .Include(p => p.Entries)
                .ThenInclude(e => e.Recipe)
            .Include(p => p.Entries)
                .ThenInclude(e => e.FoodItem)
            .FirstOrDefaultAsync();
    }

    public async Task<MealPlan?> GetActiveByUserIdAsync(Guid userId, DateOnly today)
    {
        return await _dbSet
            .Where(p => p.UserId == userId && p.StartDate <= today && p.EndDate >= today)
            .Include(p => p.Entries)
                .ThenInclude(e => e.Recipe)
            .Include(p => p.Entries)
                .ThenInclude(e => e.FoodItem)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefaultAsync();
    }
}
