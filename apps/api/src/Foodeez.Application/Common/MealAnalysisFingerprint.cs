using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Common;

/// <summary>
/// Identifies the contents of a meal so a stored AI analysis can be recognised as still
/// current. Anything the analysis is derived from - the meal type and each item's food,
/// quantity and unit - goes into the hash, so an edit to the meal invalidates its score
/// while re-opening an untouched meal reuses it.
/// </summary>
public static class MealAnalysisFingerprint
{
    /// <summary>
    /// Identifies a whole day: every meal on it plus the targets it is judged against, so
    /// logging a meal or changing a calorie goal retires the day's stored analysis.
    /// </summary>
    public static string ForDay(DateOnly date, IEnumerable<MealLog> logs, NutritionSummaryDto summary)
    {
        var canonical = new StringBuilder(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        foreach (var mealFingerprint in logs.Select(For).OrderBy(f => f, StringComparer.Ordinal))
        {
            canonical.Append('|').Append(mealFingerprint);
        }

        canonical.Append("|targets:")
            .Append(summary.TargetCalories).Append(':')
            .Append(summary.TargetProtein.ToString("R", CultureInfo.InvariantCulture)).Append(':')
            .Append(summary.TargetCarbs.ToString("R", CultureInfo.InvariantCulture)).Append(':')
            .Append(summary.TargetFat.ToString("R", CultureInfo.InvariantCulture));

        return Hash(canonical.ToString());
    }

    public static string For(MealLog mealLog)
    {
        var canonical = new StringBuilder(mealLog.MealType.ToString());

        var items = mealLog.Items
            .Select(i => (i.FoodItemId, i.Quantity, i.Unit))
            .OrderBy(i => i.FoodItemId)
            .ThenBy(i => i.Quantity)
            .ThenBy(i => i.Unit, StringComparer.Ordinal);

        foreach (var (foodItemId, quantity, unit) in items)
        {
            canonical.Append('|')
                .Append(foodItemId.ToString("N"))
                .Append(':')
                .Append(quantity.ToString("R", CultureInfo.InvariantCulture))
                .Append(':')
                .Append(unit);
        }

        return Hash(canonical.ToString());
    }

    private static string Hash(string canonical) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
}
