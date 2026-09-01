using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for home-cooked nutrition estimates.
///
/// Shared across every AI provider: each one differs only in how it ships a string of text to
/// a model and gets a string back, so the part that actually defines the feature lives here
/// once rather than being copy-pasted four times and drifting.
/// </summary>
public static class NutritionEstimation
{
    public static string BuildPrompt(EstimateNutritionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "You are a registered dietitian estimating the nutrition of a home-cooked dish. " +
            "Use standard food composition data (USDA-equivalent) and typical home cooking " +
            "practice. It is normal for home cooks not to weigh anything, so infer sensible " +
            "quantities where they are missing.");
        sb.AppendLine();
        sb.AppendLine($"Dish: {request.Name}");

        if (!string.IsNullOrWhiteSpace(request.Ingredients))
        {
            sb.AppendLine("Ingredients as described by the cook:");
            sb.AppendLine(request.Ingredients.Trim());
        }
        else
        {
            sb.AppendLine(
                "No ingredients were given. Infer a typical recipe for a dish of this name and " +
                "say so in your assumptions.");
        }

        var servings = request.Servings > 0 ? request.Servings : 1;
        sb.AppendLine($"The whole batch makes {servings} serving(s).");

        if (!string.IsNullOrWhiteSpace(request.ServingDescription))
            sb.AppendLine($"One serving is described as: {request.ServingDescription}");

        sb.AppendLine();
        sb.AppendLine("Work out the totals for the whole batch, then divide by the serving count.");
        sb.AppendLine("Report figures PER SERVING, not for the whole batch.");
        sb.AppendLine();
        sb.AppendLine("Respond with JSON only, no prose and no code fences:");
        sb.AppendLine("""
            {
              "calories": 0,
              "protein": 0,
              "carbohydrates": 0,
              "fat": 0,
              "fiber": 0,
              "sugar": 0,
              "sodium": 0,
              "servingSize": 0,
              "servingUnit": "g",
              "confidence": "low|medium|high",
              "assumptions": "one or two sentences on what you assumed",
              "assumedIngredients": ["120 g dried pasta", "..."]
            }
            """);
        sb.AppendLine();
        sb.AppendLine(
            "Units: calories in kcal; protein, carbohydrates, fat, fiber and sugar in grams; " +
            "sodium in milligrams. servingSize is a number and servingUnit its unit (use \"g\" " +
            "for solids, \"ml\" for liquids, or \"serving\" if a weight makes no sense). " +
            "Set confidence to \"high\" only when amounts were given explicitly.");

        return sb.ToString();
    }

    /// <summary>
    /// Reads the model's JSON. Returns null when nothing usable came back, so the caller can
    /// fall back to manual entry rather than showing invented numbers.
    /// </summary>
    public static EstimatedNutritionDto? Parse(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return null;

        var json = ExtractJsonObject(responseText);
        if (json == null) return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var dto = new EstimatedNutritionDto
            {
                PerServing = new NutritionalInfoDto
                {
                    Calories = Num(root, "calories"),
                    Protein = Num(root, "protein"),
                    Carbohydrates = Num(root, "carbohydrates", "carbs"),
                    Fat = Num(root, "fat"),
                    Fiber = Num(root, "fiber", "fibre"),
                    Sugar = Num(root, "sugar", "sugars"),
                    Sodium = Num(root, "sodium"),
                },
                ServingSize = Num(root, "servingSize"),
                ServingUnit = Str(root, "servingUnit") ?? "serving",
                Confidence = NormalizeConfidence(Str(root, "confidence")),
                Assumptions = Str(root, "assumptions"),
                AssumedIngredients = StrList(root, "assumedIngredients"),
                Succeeded = true,
            };

            // A dish with no calories at all means the model did not really answer.
            if (dto.PerServing.Calories <= 0) return null;

            if (dto.ServingSize <= 0)
            {
                dto.ServingSize = 1;
                dto.ServingUnit = "serving";
            }

            return dto;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Pull the first balanced {...} block out of a reply that may carry prose or fences.</summary>
    private static string? ExtractJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return text[start..(i + 1)];
        }

        return null;
    }

    private static float Num(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var v)) continue;
            if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return (float)d;
            // Models sometimes answer "12 g" instead of 12.
            if (v.ValueKind == JsonValueKind.String && TryParseLeadingNumber(v.GetString(), out var s)) return s;
        }
        return 0f;
    }

    private static bool TryParseLeadingNumber(string? raw, out float value)
    {
        value = 0f;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var span = raw.AsSpan().TrimStart();
        var end = 0;
        while (end < span.Length && (char.IsDigit(span[end]) || span[end] == '.' || span[end] == '-')) end++;

        return end > 0 && float.TryParse(span[..end], out value);
    }

    private static string? Str(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static List<string> StrList(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var v) || v.ValueKind != JsonValueKind.Array)
            return [];

        return v.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString()!)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private static string NormalizeConfidence(string? raw) => raw?.Trim().ToLowerInvariant() switch
    {
        "high" => "high",
        "medium" or "moderate" => "medium",
        _ => "low",
    };
}
