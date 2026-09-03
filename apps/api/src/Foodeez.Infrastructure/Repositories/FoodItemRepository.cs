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
        var lowerQuery = query.ToLowerInvariant();
        return await _dbSet
            .Where(f => f.Name.ToLower().Contains(lowerQuery) ||
                        (f.Brand != null && f.Brand.ToLower().Contains(lowerQuery)))
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
        var loweredName = name.Trim().ToLowerInvariant();
        var loweredUnit = servingUnit.Trim().ToLowerInvariant();

        return await _dbSet.FirstOrDefaultAsync(f =>
            f.IsCustom &&
            f.CreatedByUserId == userId &&
            f.Name.ToLower() == loweredName &&
            f.ServingUnit.ToLower() == loweredUnit);
    }

    public async Task<IReadOnlyList<FoodItem>> GetByFdcIdsAsync(IEnumerable<int> fdcIds)
    {
        var ids = fdcIds.ToList();
        return await _dbSet
            .Where(f => f.FdcId != null && ids.Contains(f.FdcId.Value))
            .ToListAsync();
    }
}
