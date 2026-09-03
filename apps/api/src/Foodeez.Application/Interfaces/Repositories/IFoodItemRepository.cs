using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IFoodItemRepository : IBaseRepository<FoodItem>
{
    Task<IReadOnlyList<FoodItem>> SearchAsync(string query, int limit = 20);
    Task<FoodItem?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<FoodItem>> GetByFdcIdsAsync(IEnumerable<int> fdcIds);

    /// <summary>
    /// A food this user has already had estimated under exactly this name and unit, if any.
    /// Quick add reuses it rather than filing a fresh "Turkey sandwich" every single morning.
    /// </summary>
    Task<FoodItem?> FindCustomByNameAsync(Guid userId, string name, string servingUnit);
}
