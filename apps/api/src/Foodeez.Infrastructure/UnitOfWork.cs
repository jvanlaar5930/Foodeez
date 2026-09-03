using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public IMealLogRepository MealLogs { get; }
    public IDayAnalysisRepository DayAnalyses { get; }
    public IFoodItemRepository FoodItems { get; }
    public IMealTemplateRepository MealTemplates { get; }
    public IMealPlanRepository MealPlans { get; }
    public IRecipeRepository Recipes { get; }
    public ISavedRecipeRepository SavedRecipes { get; }
    public IChatRepository Chat { get; }
    public IGroceryListRepository GroceryLists { get; }
    public IAppSettingRepository AppSettings { get; }
    public IAppLogRepository AppLogs { get; }

    public UnitOfWork(
        AppDbContext context,
        IUserRepository users,
        IMealLogRepository mealLogs,
        IDayAnalysisRepository dayAnalyses,
        IFoodItemRepository foodItems,
        IMealTemplateRepository mealTemplates,
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        ISavedRecipeRepository savedRecipes,
        IChatRepository chat,
        IGroceryListRepository groceryLists,
        IAppSettingRepository appSettings,
        IAppLogRepository appLogs)
    {
        _context = context;
        Users = users;
        MealLogs = mealLogs;
        DayAnalyses = dayAnalyses;
        FoodItems = foodItems;
        MealTemplates = mealTemplates;
        MealPlans = mealPlans;
        Recipes = recipes;
        SavedRecipes = savedRecipes;
        Chat = chat;
        GroceryLists = groceryLists;
        AppSettings = appSettings;
        AppLogs = appLogs;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "The save affected fewer rows than expected - it may have already applied.", ex);
        }
    }
}
