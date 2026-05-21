using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class MealLogRepository : BaseRepository<MealLog>, IMealLogRepository
{
    public MealLogRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<MealLog>> GetByUserAndDateAsync(Guid userId, DateOnly date)
    {
        return await _dbSet
            .Where(m => m.UserId == userId && m.LogDate == date)
            .Include(m => m.Items)
                .ThenInclude(i => i.FoodItem)
            .OrderBy(m => m.MealType)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MealLog>> GetByUserAndDateRangeAsync(Guid userId, DateOnly start, DateOnly end)
    {
        return await _dbSet
            .Where(m => m.UserId == userId && m.LogDate >= start && m.LogDate <= end)
            .Include(m => m.Items)
                .ThenInclude(i => i.FoodItem)
            .OrderBy(m => m.LogDate)
            .ThenBy(m => m.MealType)
            .ToListAsync();
    }
}
