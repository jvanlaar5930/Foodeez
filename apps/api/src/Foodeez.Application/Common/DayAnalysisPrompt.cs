using System.Globalization;
using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for a whole day's analysis.
///
/// Shared across every AI provider for the same reason as <see cref="NutritionEstimation"/>:
/// the providers differ only in how they ship a string to a model, so what actually defines
/// the feature lives here once.
/// </summary>
public static class DayAnalysisPrompt
{
    public static string Build(DayAnalysisRequest request)
    {
        var summary = request.Summary;
        var sb = new StringBuilder();

        sb.AppendLine(
            "You are a registered dietitian reviewing what someone has eaten so far on a single day. " +
            "Be encouraging but honest, and judge the day as a whole rather than meal by meal.");
        sb.AppendLine();
        sb.AppendLine($"Date: {request.Date:yyyy-MM-dd}{(request.IsToday ? " (today, the day is not over)" : " (a past day)")}");

        if (!string.IsNullOrWhiteSpace(request.DietaryGoal))
        {
            sb.AppendLine($"Goal: {request.DietaryGoal}");
        }

        sb.AppendLine();
        sb.AppendLine("Meals logged so far:");

        if (request.Meals.Count == 0)
        {
            sb.AppendLine("  (nothing logged yet)");
        }

        foreach (var meal in request.Meals)
        {
            sb.AppendLine($"  {meal.MealType}: {string.Join(", ", meal.Items)}");
            sb.AppendLine(
                $"    {Round(meal.Calories)} kcal, {Round(meal.Protein)}g protein, " +
                $"{Round(meal.Carbs)}g carbs, {Round(meal.Fat)}g fat, {Round(meal.Fiber)}g fiber");
        }

        sb.AppendLine();
        sb.AppendLine("Day totals against targets:");
        sb.AppendLine($"  Calories: {Round(summary.TotalCalories)} / {summary.TargetCalories} kcal");
        sb.AppendLine($"  Protein:  {Round(summary.TotalProtein)} / {Round(summary.TargetProtein)} g");
        sb.AppendLine($"  Carbs:    {Round(summary.TotalCarbs)} / {Round(summary.TargetCarbs)} g");
        sb.AppendLine($"  Fat:      {Round(summary.TotalFat)} / {Round(summary.TargetFat)} g");
        sb.AppendLine($"  Fiber:    {Round(summary.TotalFiber)} g (aim for 25-35g)");

        sb.AppendLine();
        sb.AppendLine(
            "Score the day 0-100 on how complete and balanced it is so far, judged against the targets " +
            "and against variety across food groups (vegetables, fruit, whole grains, protein, healthy fats).");
        sb.AppendLine(
            request.IsToday
                ? "Then recommend what to eat for the REST of today to finish the day complete and balanced. " +
                  "Name the meal or snack, the food, and a rough portion, and keep the calories within what is " +
                  "left of the target."
                : "Then say what could have been eaten differently to make that day complete and balanced.");
        sb.AppendLine("Give 2-4 recommendations. Be specific about foods and portions - never generic advice like 'eat healthier'.");

        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine();
        sb.AppendLine("JSON shape:");
        sb.AppendLine(@"{
  ""score"": 64,
  ""status"": ""1-2 sentences on where the day stands"",
  ""gaps"": [""42g short on protein"", ""No vegetables yet""],
  ""recommendations"": [""Dinner: 150g grilled salmon with a cup of quinoa and roasted broccoli (~550 kcal)"", ""Evening snack: Greek yoghurt with berries for the last 20g of protein""]
}");

        return sb.ToString();
    }

    /// <summary>Reads the model's JSON back, or null when it returned nothing usable.</summary>
    public static DayAnalysisDto? Parse(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd <= jsonStart)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(responseText[jsonStart..(jsonEnd + 1)]);
            var root = doc.RootElement;

            var dto = new DayAnalysisDto();

            if (root.TryGetProperty("score", out var score) && score.TryGetInt32(out var scoreValue))
            {
                dto.Score = Math.Clamp(scoreValue, 0, 100);
            }

            if (root.TryGetProperty("status", out var status))
            {
                dto.Status = status.GetString() ?? string.Empty;
            }

            dto.Gaps = ReadStrings(root, "gaps");
            dto.Recommendations = ReadStrings(root, "recommendations");

            return string.IsNullOrWhiteSpace(dto.Status) && dto.Recommendations.Count == 0 ? null : dto;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<string> ReadStrings(JsonElement root, string property) =>
        root.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList()
            : new List<string>();

    private static string Round(float value) =>
        MathF.Round(value).ToString(CultureInfo.InvariantCulture);
}
