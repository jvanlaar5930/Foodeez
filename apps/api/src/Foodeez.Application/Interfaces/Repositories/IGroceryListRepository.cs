using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IGroceryListRepository : IBaseRepository<GroceryList>
{
    /// <summary>The list already compiled for exactly this range, with its items.</summary>
    Task<GroceryList?> GetByRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate);

    Task<GroceryList?> GetWithItemsAsync(Guid listId);

    Task AddItemAsync(GroceryListItem item);

    void RemoveItem(GroceryListItem item);
}
