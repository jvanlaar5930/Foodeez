using Foodeez.Application.Common;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Grocery;

/// <summary>
/// Gathers the meals planned across a stretch of dates, from whichever plans cover them.
///
/// A shopping list is asked for by date range, not by plan: someone shopping for the week
/// does not care that Monday and Thursday came from two different plans, and asking them to
/// pick one would leave half the week unbought.
/// </summary>
public class PlannedMealReader
{
    private readonly IUnitOfWork _unitOfWork;

    public PlannedMealReader(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PlannedMealSummary>> ReadAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var plans = await _unitOfWork.MealPlans.GetByUserIdAsync(userId);

        var entries = plans
            .SelectMany(p => p.Entries)
            .Where(e => e.EntryDate >= startDate && e.EntryDate <= endDate)
            .ToList();

        var ingredientsByRecipe = await LoadIngredientsAsync(entries);

        return entries
            .Select(entry => new PlannedMealSummary(
                entry.EntryDate,
                entry.MealType,
                Label(entry),
                entry.Servings,
                entry.RecipeId is { } recipeId && ingredientsByRecipe.TryGetValue(recipeId, out var known)
                    ? known
                    : Array.Empty<string>()))
            .Where(meal => meal.Label.Length > 0)
            // Overlapping plans can cover the same date, and buying twice for one dinner is
            // worse than the duplicate looking tidy in the calendar.
            .GroupBy(meal => (meal.Date, meal.MealType, meal.Label))
            .Select(group => group.First())
            .OrderBy(meal => meal.Date)
            .ThenBy(meal => meal.MealType)
            .ToList();
    }

    /// <summary>
    /// Ingredient lines for the entries that have a real recipe behind them. Most planned
    /// meals do not - the AI-generated ones are a name in a notes field - so this is usually
    /// a small lookup, and the model is left to reason about the rest.
    /// </summary>
    private async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadIngredientsAsync(
        IReadOnlyList<MealPlanEntry> entries)
    {
        var recipeIds = entries
            .Where(e => e.RecipeId.HasValue)
            .Select(e => e.RecipeId!.Value)
            .Distinct()
            .ToList();

        var byRecipe = new Dictionary<Guid, IReadOnlyList<string>>();

        foreach (var recipeId in recipeIds)
        {
            var recipe = await _unitOfWork.Recipes.GetByIdAsync(recipeId);
            if (recipe == null)
            {
                continue;
            }

            byRecipe[recipeId] = recipe.Ingredients
                .Select(Describe)
                .Where(line => line.Length > 0)
                .ToList();
        }

        return byRecipe;
    }

    private static string Describe(RecipeIngredient ingredient)
    {
        var name = ingredient.FoodItem?.Name ?? ingredient.IngredientName ?? string.Empty;
        if (name.Length == 0)
        {
            return string.Empty;
        }

        var amount = $"{ingredient.Quantity:0.##} {ingredient.Unit}".Trim();
        return amount.Length == 0 ? name : $"{amount} {name}";
    }

    /// <summary>
    /// What the meal is called. Recipe and food-item names come from the catalogue; the
    /// notes field is where every AI-generated meal keeps its name, so it is not a fallback
    /// but the common case.
    /// </summary>
    private static string Label(MealPlanEntry entry) =>
        entry.Recipe?.Name
        ?? entry.FoodItem?.Name
        ?? entry.Notes?.Trim()
        ?? string.Empty;
}
