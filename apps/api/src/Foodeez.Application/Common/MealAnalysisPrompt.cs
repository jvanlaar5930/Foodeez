using System.Globalization;
using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for a single meal's analysis, written for the streaming
/// path: it leads with prose so there is something to watch, then the JSON the app stores.
/// </summary>
public static class MealAnalysisPrompt
{
    public static string Build(MealAnalysisRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional nutritionist. Analyze this meal for nutritional completeness and balance.");
        sb.AppendLine();
        sb.AppendLine($"Meal type: {request.MealType}");
        sb.AppendLine("Items:");

        var totals = (Calories: 0f, Protein: 0f, Carbs: 0f, Fat: 0f, Fiber: 0f);
        foreach (var item in request.Items)
        {
            sb.AppendLine(
                $"  - {item.Amount}{item.Unit} {item.Name}: {MathF.Round(item.Calories)} kcal, " +
                $"{MathF.Round(item.Protein)}g protein, {MathF.Round(item.Carbs)}g carbs, " +
                $"{MathF.Round(item.Fat)}g fat, {MathF.Round(item.Fiber)}g fiber");

            totals.Calories += item.Calories;
            totals.Protein += item.Protein;
            totals.Carbs += item.Carbs;
            totals.Fat += item.Fat;
            totals.Fiber += item.Fiber;
        }

        sb.AppendLine();
        sb.AppendLine(
            $"Totals: {MathF.Round(totals.Calories)} kcal | {MathF.Round(totals.Protein)}g protein | " +
            $"{MathF.Round(totals.Carbs)}g carbs | {MathF.Round(totals.Fat)}g fat | {MathF.Round(totals.Fiber)}g fiber");
        sb.AppendLine();
        sb.AppendLine(
            "Assess whether this is a nutritionally complete and balanced meal: macro balance, " +
            "micronutrients, fiber, and whether it suits the meal type. Score it 0-100 " +
            "(100 = perfectly balanced) and keep suggestions specific and actionable " +
            "(for example 'Add a handful of spinach').");
        if (request.ExcludedFoods.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(AINarration.ExclusionLine(request.ExcludedFoods));
        }

        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine();
        sb.AppendLine("JSON shape:");
        sb.AppendLine(@"{
  ""score"": 72,
  ""completeness"": ""1-2 sentence overall assessment"",
  ""missing"": [""Fiber"", ""Vegetables""],
  ""suggestions"": [""Add a side salad for fiber and micronutrients"", ""Include olive oil for healthy fats""]
}");

        return sb.ToString();
    }

    /// <summary>Reads the model's JSON back, or null when it returned nothing usable.</summary>
    public static MealAnalysisDto? Parse(string responseText)
    {
        var root = JsonExtraction.ReadObject(responseText);
        if (root == null)
        {
            return null;
        }

        var element = root.Value;
        var dto = new MealAnalysisDto
        {
            Score = JsonExtraction.ReadInt(element, "score"),
            Completeness = JsonExtraction.ReadString(element, "completeness"),
            Missing = JsonExtraction.ReadStrings(element, "missing"),
            Suggestions = JsonExtraction.ReadStrings(element, "suggestions")
        };

        dto.Score = Math.Clamp(dto.Score, 0, 100);

        return string.IsNullOrWhiteSpace(dto.Completeness) && dto.Suggestions.Count == 0 ? null : dto;
    }
}

/// <summary>
/// The small amount of JSON archaeology every AI response needs: models wrap their object in
/// stray prose however often you ask them not to, and a missing or mistyped field must read
/// as "not given" rather than throw.
/// </summary>
public static class JsonExtraction
{
    /// <summary>The outermost JSON object in a model's answer, or null when there isn't one.</summary>
    public static JsonElement? ReadObject(string responseText)
    {
        var span = FindBalancedObject(responseText);
        if (span == null)
        {
            return null;
        }

        try
        {
            // Clone: the document is disposed here, and an un-cloned element dies with it.
            using var doc = JsonDocument.Parse(span);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// The text from the first "{" to the "}" that actually closes it, tracking nesting and
    /// skipping braces inside string literals. A model asked for "JSON, nothing after it"
    /// still sometimes adds a closing remark of its own - `responseText.LastIndexOf('}')`
    /// would grab a brace out of that instead of the real end of the object, and a response
    /// cut short by a token limit never reaches a matching brace at all, which this reports
    /// as absent rather than silently parsing a truncated slice.
    /// </summary>
    private static string? FindBalancedObject(string responseText)
    {
        var start = responseText.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < responseText.Length; i++)
        {
            var c = responseText[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': depth++; break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return responseText[start..(i + 1)];
                    }
                    break;
            }
        }

        // Depth never returned to zero - the object was cut off before it closed.
        return null;
    }

    public static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>
    /// A number, whether the model wrote it as one or quoted it.
    ///
    /// Two things make the obvious one-liner wrong. TryGetInt32 does not return false for a
    /// non-number - it throws, so the value kind has to be checked before asking. And models
    /// do quote numbers ("350"), which is why LocalAIService grew its own lenient reader;
    /// treating that as absent silently zeroed a calorie count, and letting the throw escape
    /// discarded the entire response, since nothing above here catches it.
    /// </summary>
    public static int ReadInt(JsonElement element, string property, int fallback = 0)
    {
        if (!element.TryGetProperty(property, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.Number)
            return value.TryGetInt32(out var parsed) ? parsed : fallback;

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fromText))
            return fromText;

        return fallback;
    }

    /// <inheritdoc cref="ReadInt"/>
    public static float ReadFloat(JsonElement element, string property, float fallback = 0f)
    {
        if (!element.TryGetProperty(property, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.Number)
            return value.TryGetSingle(out var parsed) ? parsed : fallback;

        if (value.ValueKind == JsonValueKind.String &&
            float.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var fromText))
            return fromText;

        return fallback;
    }

    public static List<string> ReadStrings(JsonElement element, string property) =>
        element.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
                .Select(e => e.ValueKind == JsonValueKind.String ? e.GetString() ?? string.Empty : string.Empty)
                .Where(s => s.Length > 0)
                .ToList()
            : new List<string>();
}
