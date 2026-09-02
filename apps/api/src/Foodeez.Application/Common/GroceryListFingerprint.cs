using System.Security.Cryptography;
using System.Text;

namespace Foodeez.Application.Common;

/// <summary>
/// Identifies the planned meals a grocery list was compiled from, so the app can tell when
/// the plan has moved on without it. The same idea as the meal and day analyses, with one
/// difference: a stale list is not thrown away, because someone may be halfway through
/// shopping from it.
/// </summary>
public static class GroceryListFingerprint
{
    public static string For(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<PlannedMealSummary> meals,
        IReadOnlyCollection<string> excludedFoods)
    {
        var canonical = new StringBuilder();
        canonical.Append(startDate.ToString("yyyy-MM-dd")).Append('|');
        canonical.Append(endDate.ToString("yyyy-MM-dd")).Append('|');

        // Ordered, so the same week described in a different order is the same week.
        foreach (var meal in meals
                     .OrderBy(m => m.Date)
                     .ThenBy(m => m.MealType)
                     .ThenBy(m => m.Label, StringComparer.Ordinal))
        {
            canonical.Append(meal.Date.ToString("yyyy-MM-dd")).Append(':')
                .Append((int)meal.MealType).Append(':')
                .Append(meal.Label).Append(':')
                .Append(meal.Servings.ToString("0.##")).Append(':')
                .Append(string.Join(",", meal.KnownIngredients))
                .Append('|');
        }

        foreach (var food in excludedFoods.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            canonical.Append("x:").Append(food.Trim().ToLowerInvariant()).Append('|');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())))
            .ToLowerInvariant();
    }
}
