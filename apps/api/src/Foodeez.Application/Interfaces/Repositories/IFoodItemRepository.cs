using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IFoodItemRepository : IBaseRepository<FoodItem>
{
    Task<IReadOnlyList<FoodItem>> SearchAsync(string query, int limit = 20);
    Task<FoodItem?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<FoodItem>> GetByFdcIdsAsync(IEnumerable<int> fdcIds);
}
