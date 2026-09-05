using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class FoodItemRepository : BaseRepository<FoodItem>, IFoodItemRepository
{
    public FoodItemRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<FoodItem>> SearchAsync(string query, int limit = 20)
    {
        // Tracked: a match found here is attached to the meal log being built from it.
        // No ToLower(): MySQL's collation compares case-insensitively already, and calling
        // it wraps the column in a function no index can be used through.
        return await _dbSet
            .Where(f => f.Name.Contains(query) ||
                        (f.Brand != null && f.Brand.Contains(query)))
            .Take(limit)
            .ToListAsync();
    }

    public async Task<FoodItem?> GetByBarcodeAsync(string barcode)
    {
        return await _dbSet
            .FirstOrDefaultAsync(f => f.Barcode == barcode);
    }

    public async Task<FoodItem?> FindCustomByNameAsync(Guid userId, string name, string servingUnit)
    {
        var wantedName = name.Trim();
        var wantedUnit = servingUnit.Trim();

        return await _dbSet.FirstOrDefaultAsync(f =>
            f.IsCustom &&
            f.CreatedByUserId == userId &&
            f.Name == wantedName &&
            f.ServingUnit == wantedUnit);
    }

    public async Task<IReadOnlyList<FoodItem>> GetByFdcIdsAsync(IEnumerable<int> fdcIds)
    {
        var ids = fdcIds.ToList();
        return await _dbSet
            .Where(f => f.FdcId != null && ids.Contains(f.FdcId.Value))
            .ToListAsync();
    }
}
