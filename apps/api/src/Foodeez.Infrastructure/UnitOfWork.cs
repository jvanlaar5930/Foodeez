using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Infrastructure.Data;

namespace Foodeez.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public IMealLogRepository MealLogs { get; }
    public IDayAnalysisRepository DayAnalyses { get; }
    public IFoodItemRepository FoodItems { get; }
    public IMealPlanRepository MealPlans { get; }
    public IRecipeRepository Recipes { get; }
    public ISavedRecipeRepository SavedRecipes { get; }
    public IAppSettingRepository AppSettings { get; }
    public IAppLogRepository AppLogs { get; }

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        IMealLogRepository mealLogs,
        IDayAnalysisRepository dayAnalyses,
        IFoodItemRepository foodItems,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        ISavedRecipeRepository savedRecipes,
        IAppSettingRepository appSettings,
        IAppLogRepository appLogs)
    {
        _context = context;
        Users = users;
        MealLogs = mealLogs;
        DayAnalyses = dayAnalyses;
        FoodItems = foodItems;
        MealPlans = mealPlans;
        Recipes = recipes;
        SavedRecipes = savedRecipes;
        AppSettings = appSettings;
        AppLogs = appLogs;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }
}
