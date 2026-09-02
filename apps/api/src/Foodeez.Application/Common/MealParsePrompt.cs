using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;

namespace Foodeez.Application.Common;

/// <summary>
/// Turns a meal somebody described - in a sentence, or in a photograph - into a list of
/// separate foods with amounts.
///
/// The model is asked to do one job here: split the meal up and put a number on each part.
/// Its nutrition figures are a fallback, because the caller looks every name up in the food
/// database first and prefers real data wherever it finds a match. That split is deliberate:
/// splitting a sandwich into bread, turkey and mayo is something a language model is reliably
/// good at, and recalling the sodium content of a slice of rye is not.
/// </summary>
public static class MealParsePrompt
{
    /// <summary>The most items one meal may contribute, so a rambling description cannot run up a bill.</summary>
    public const int MaxItems = 20;

    public static string BuildText(string description)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a dietitian's assistant. Break the meal described below into the separate");
        sb.AppendLine("foods it is made of, and give the amount of each one.");
        sb.AppendLine();
        sb.AppendLine("Meal as described:");
        sb.AppendLine(description);
        sb.AppendLine();
        AppendRules(sb);
        return sb.ToString();
    }

    public static string BuildImage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a dietitian's assistant. List the separate foods visible in this photograph");
        sb.AppendLine("of a meal, and estimate the amount of each one from what you can see - use the plate,");
        sb.AppendLine("cutlery or packaging for scale.");
        sb.AppendLine();
        sb.AppendLine("Include only food and drink. Ignore the plate, table and background.");
        sb.AppendLine();
        AppendRules(sb);
        return sb.ToString();
    }

    private static void AppendRules(StringBuilder sb)
    {
        sb.AppendLine("Rules:");
        sb.AppendLine("- One entry per distinct food. A sandwich is its bread, its filling and its spread,");
        sb.AppendLine("  not a single entry, unless it was named as a branded or restaurant item.");
        sb.AppendLine("- Do not add anything that was not described or shown. A meal of two items has two entries.");
        sb.AppendLine($"- At most {MaxItems} entries.");
        sb.AppendLine("- Use the unit the food is naturally counted in: 'slice', 'piece', 'cup', 'tbsp', 'g', 'ml'.");
        sb.AppendLine("- Where no amount was given, assume one ordinary serving and say what you assumed in 'note'.");
        sb.AppendLine("- The nutrition figures are for the whole amount in 'quantity', NOT per 100g and NOT per serving.");
        sb.AppendLine("- 'confidence' is 0.0-1.0: how sure you are this food and amount are what was meant.");
        sb.AppendLine("- Set 'brand' only when a brand was actually named.");
        sb.AppendLine();
        sb.AppendLine("Reply with JSON only - no prose, no code fences.");
        sb.AppendLine();
        sb.AppendLine("JSON shape:");
        sb.AppendLine(@"{
  ""items"": [
    {
      ""name"": ""Whole wheat bread"",
      ""brand"": null,
      ""quantity"": 2,
      ""unit"": ""slice"",
      ""calories"": 160,
      ""protein"": 8,
      ""carbohydrates"": 28,
      ""fat"": 2,
      ""fiber"": 4,
      ""sugar"": 3,
      ""sodium"": 300,
      ""confidence"": 0.9
    }
  ],
  ""note"": ""Assumed a standard 2-slice sandwich.""
}");
    }

    /// <summary>
    /// Reads the model's answer back. Returns an empty list when nothing usable came out,
    /// which the caller must treat as "could not read this", never as "this meal is empty".
    /// </summary>
    public static ParsedMealDto Parse(string responseText)
    {
        var root = JsonExtraction.ReadObject(responseText);
        if (root is not { } element)
        {
            return new ParsedMealDto();
        }

        var result = new ParsedMealDto { Note = NullIfBlank(JsonExtraction.ReadString(element, "note")) };

        if (!element.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in items.EnumerateArray().Take(MaxItems))
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var name = JsonExtraction.ReadString(item, "name").Trim();
            if (name.Length == 0)
            {
                continue;
            }

            // A zero or missing quantity would divide the whole meal down to nothing later on,
            // so an unusable one becomes a single serving rather than a silent zero.
            var quantity = JsonExtraction.ReadFloat(item, "quantity", 1f);
            if (quantity <= 0f || float.IsNaN(quantity))
            {
                quantity = 1f;
            }

            var unit = JsonExtraction.ReadString(item, "unit").Trim();

            result.Items.Add(new ParsedFoodDto
            {
                Name = name,
                Brand = NullIfBlank(JsonExtraction.ReadString(item, "brand")),
                ServingSize = quantity,
                ServingUnit = unit.Length == 0 ? "serving" : unit.ToLowerInvariant(),
                Confidence = Math.Clamp(JsonExtraction.ReadFloat(item, "confidence", 0.5f), 0f, 1f),
                NutritionalInfo = new DTOs.MealLogs.NutritionalInfoDto
                {
                    Calories = NonNegative(JsonExtraction.ReadFloat(item, "calories")),
                    Protein = NonNegative(JsonExtraction.ReadFloat(item, "protein")),
                    Carbohydrates = NonNegative(JsonExtraction.ReadFloat(item, "carbohydrates")),
                    Fat = NonNegative(JsonExtraction.ReadFloat(item, "fat")),
                    Fiber = NonNegative(JsonExtraction.ReadFloat(item, "fiber")),
                    Sugar = NonNegative(JsonExtraction.ReadFloat(item, "sugar")),
                    Sodium = NonNegative(JsonExtraction.ReadFloat(item, "sodium")),
                }
            });
        }

        return result;
    }

    private static float NonNegative(float value) => float.IsNaN(value) || value < 0f ? 0f : value;

    private static string? NullIfBlank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
