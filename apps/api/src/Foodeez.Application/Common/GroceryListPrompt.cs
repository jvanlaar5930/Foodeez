using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.Grocery;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.Common;

/// <summary>One planned meal, reduced to what a shopping list needs to know about it.</summary>
public sealed record PlannedMealSummary(
    DateOnly Date,
    MealType MealType,
    string Label,
    float Servings,
    IReadOnlyList<string> KnownIngredients);

/// <summary>
/// Turns a stretch of the meal plan into a shopping list.
///
/// Most planned meals are a line of text - "Lemon herb salmon with roasted vegetables" - with
/// no recipe row behind them, so working out what to buy is a judgement call about portions
/// and staples rather than a sum over ingredient rows. Where a real recipe is attached its
/// ingredients are given to the model as fact, to be used rather than guessed at.
/// </summary>
public static class GroceryListPrompt
{
    /// <summary>
    /// The aisles the list is grouped by. The model is asked to use these and nothing else,
    /// so the grouping stays stable between one week's list and the next.
    /// </summary>
    public static readonly string[] Categories =
    {
        "Produce",
        "Meat & Seafood",
        "Dairy & Eggs",
        "Bakery",
        "Pantry",
        "Frozen",
        "Drinks",
        "Other"
    };

    public static string Build(
        IReadOnlyList<PlannedMealSummary> meals,
        IReadOnlyCollection<string> excludedFoods,
        DateOnly startDate,
        DateOnly endDate)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are writing a grocery shopping list for the meals below.");
        sb.AppendLine($"They cover {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}.");
        sb.AppendLine();

        sb.AppendLine("Planned meals:");
        foreach (var meal in meals)
        {
            sb.Append($"- {meal.Date:ddd d MMM} {meal.MealType}: {meal.Label}");
            if (meal.Servings != 1f)
            {
                sb.Append($" (x{meal.Servings:0.##} servings)");
            }

            if (meal.KnownIngredients.Count > 0)
            {
                sb.Append($" [known ingredients: {string.Join("; ", meal.KnownIngredients)}]");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- Combine the same ingredient across meals into one line with a total amount.");
        sb.AppendLine("- Where a meal lists known ingredients, use those rather than guessing what is in it.");
        sb.AppendLine("- Write amounts the way they would be bought, not to the gram: \"500 g\", \"2 bunches\", \"1 loaf\".");
        sb.AppendLine("- Skip water, salt and pepper. Include other staples only when the meals need a real amount of them.");
        sb.AppendLine($"- Use only these categories: {string.Join(", ", Categories)}.");
        sb.AppendLine("- In \"source\", name the meals that need the item, briefly.");

        var exclusions = AINarration.ExclusionLine(excludedFoods);
        if (exclusions.Length > 0)
        {
            sb.AppendLine($"- {exclusions}");
        }

        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine("In those sentences, say what the shop looks like overall - roughly how much fresh food, anything worth buying early - not a reading of the list.");
        sb.AppendLine();
        sb.AppendLine("JSON shape:");
        sb.AppendLine(@"{
  ""items"": [
    { ""name"": ""Salmon fillets"", ""quantity"": ""4 fillets"", ""category"": ""Meat & Seafood"", ""source"": ""Mon dinner, Thu dinner"" },
    { ""name"": ""Broccoli"", ""quantity"": ""2 heads"", ""category"": ""Produce"", ""source"": ""Mon dinner"" }
  ]
}");

        return sb.ToString();
    }

    /// <summary>Reads the list back. An empty result means nothing usable came out.</summary>
    public static List<GroceryItemDto> Parse(string responseText)
    {
        var items = new List<GroceryItemDto>();

        var root = JsonExtraction.ReadObject(responseText);
        if (root == null
            || !root.Value.TryGetProperty("items", out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return items;
        }

        var order = 0;
        foreach (var element in array.EnumerateArray())
        {
            var name = JsonExtraction.ReadString(element, "name");
            if (name.Length == 0)
            {
                continue;
            }

            var source = JsonExtraction.ReadString(element, "source");

            items.Add(new GroceryItemDto
            {
                Name = Truncate(name, 200),
                Quantity = Truncate(JsonExtraction.ReadString(element, "quantity"), 100),
                Category = NormalizeCategory(JsonExtraction.ReadString(element, "category")),
                Source = source.Length > 0 ? Truncate(source, 500) : null,
                SortOrder = order++
            });
        }

        return items;
    }

    /// <summary>
    /// Snaps a category onto the known list. A model that answers "Vegetables" or "produce"
    /// would otherwise create an aisle of its own that sorts somewhere unhelpful.
    /// </summary>
    public static string NormalizeCategory(string category)
    {
        var trimmed = category.Trim();
        if (trimmed.Length == 0)
        {
            return "Other";
        }

        var match = Categories.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        return match ?? "Other";
    }

    /// <summary>Where a category sits in a shop, so the list is walked rather than hunted through.</summary>
    public static int CategoryRank(string category)
    {
        var index = Array.FindIndex(Categories, c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
        return index < 0 ? Categories.Length : index;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
