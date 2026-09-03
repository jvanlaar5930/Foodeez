using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Common;

/// <summary>
/// Identifies what an AI analysis was made for, so a stored one can be recognised as still
/// current. Everything the analysis was derived from goes into the hash - the meal itself,
/// the targets it is judged against, and the foods the person avoids - so any of them
/// changing retires the stored score, while re-opening an untouched meal reuses it.
///
/// The exclusions matter most: a suggestion written before someone recorded a peanut allergy
/// must not go on being served afterwards.
/// </summary>
public static class MealAnalysisFingerprint
{
    /// <summary>
    /// Identifies a whole day: every meal on it plus the targets it is judged against, so
    /// logging a meal or changing a calorie goal retires the day's stored analysis.
    /// </summary>
    public static string ForDay(
        DateOnly date,
        IEnumerable<MealLog> logs,
        NutritionSummaryDto summary,
        IEnumerable<string> excludedFoods)
    {
        var canonical = new StringBuilder(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        foreach (var mealFingerprint in logs
                     .Select(log => For(log, excludedFoods))
                     .OrderBy(f => f, StringComparer.Ordinal))
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

    public static string For(MealLog mealLog, IEnumerable<string> excludedFoods)
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

        canonical.Append("|avoids:");
        foreach (var food in excludedFoods
                     .Select(food => food.Trim().ToLowerInvariant())
                     .OrderBy(food => food, StringComparer.Ordinal))
        {
            canonical.Append(food).Append(';');
        }

        return Hash(canonical.ToString());
    }

    private static string Hash(string canonical) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
}
