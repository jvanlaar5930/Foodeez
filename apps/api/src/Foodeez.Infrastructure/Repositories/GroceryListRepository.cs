using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class GroceryListRepository : BaseRepository<GroceryList>, IGroceryListRepository
{
    public GroceryListRepository(AppDbContext context) : base(context) { }

    public async Task<GroceryList?> GetByRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        return await WithItems()
            .FirstOrDefaultAsync(g => g.UserId == userId && g.StartDate == startDate && g.EndDate == endDate);
    }

    public async Task<GroceryList?> GetWithItemsAsync(Guid listId)
    {
        return await WithItems().FirstOrDefaultAsync(g => g.Id == listId);
    }

    public async Task AddItemAsync(GroceryListItem item)
    {
        await _context.Set<GroceryListItem>().AddAsync(item);
    }

    public void RemoveItem(GroceryListItem item)
    {
        _context.Set<GroceryListItem>().Remove(item);
    }

    private IQueryable<GroceryList> WithItems() =>
        _dbSet.Include(g => g.Items.OrderBy(i => i.SortOrder).ThenBy(i => i.Name));
}
