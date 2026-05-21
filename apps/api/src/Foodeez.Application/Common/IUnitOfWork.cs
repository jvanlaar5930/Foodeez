using Foodeez.Application.Interfaces.Repositories;

namespace Foodeez.Application.Common;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IMealLogRepository MealLogs { get; }
    IFoodItemRepository FoodItems { get; }
    IMealPlanRepository MealPlans { get; }
    IRecipeRepository Recipes { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
