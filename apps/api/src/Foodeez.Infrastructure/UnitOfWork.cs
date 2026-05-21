using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Infrastructure.Data;

namespace Foodeez.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public IMealLogRepository MealLogs { get; }
    public IFoodItemRepository FoodItems { get; }
    public IMealPlanRepository MealPlans { get; }
    public IRecipeRepository Recipes { get; }

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        IMealLogRepository mealLogs,
        IFoodItemRepository foodItems,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes)
    {
        _context = context;
        Users = users;
        MealLogs = mealLogs;
        FoodItems = foodItems;
        MealPlans = mealPlans;
        Recipes = recipes;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
