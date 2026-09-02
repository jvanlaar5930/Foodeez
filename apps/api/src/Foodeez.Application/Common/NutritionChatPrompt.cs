using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt behind the advice tab, and the reading of what comes back.
///
/// Unlike the other AI features here the prose *is* the product - it is the answer the person
/// reads - so the JSON at the end is optional and carries only the one thing prose cannot: a
/// set of meals in the exact shape a calendar slot needs, for when the conversation has turned
/// into a plan.
/// </summary>
public static class NutritionChatPrompt
{
    /// <summary>How much of a thread is sent back. Long enough to hold a plan being worked
    /// out over several turns, short enough that a months-old thread does not cost a fortune.</summary>
    public const int HistoryTurns = 20;

    public static string Build(
        UserProfileDto? profile,
        IReadOnlyList<ChatMessage> history,
        string question,
        DateOnly today)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a knowledgeable, practical nutrition coach having a conversation with someone about their eating.");
        sb.AppendLine("Answer in plain language, be specific, and keep it short - a few sentences unless they asked for detail.");
        sb.AppendLine("Never invent facts about what they have eaten; if you need to know something, ask.");
        sb.AppendLine();
        sb.AppendLine($"Today is {today:dddd, d MMMM yyyy} ({today:yyyy-MM-dd}).");
        sb.AppendLine();

        AppendProfile(sb, profile);
        AppendHistory(sb, history);

        sb.AppendLine("Their message:");
        sb.AppendLine(question);
        sb.AppendLine();

        sb.AppendLine("Reply in plain sentences. Do not use markdown, code fences, headings or bullet characters.");
        sb.AppendLine();
        sb.AppendLine("If - and only if - they have asked you to plan meals, or agreed to a plan you offered, ");
        sb.AppendLine("finish your reply with a JSON object on its own containing the meals, and write nothing after it:");
        sb.AppendLine(@"{
  ""plannedMeals"": [
    {
      ""date"": ""2025-01-06"",
      ""mealType"": ""Dinner"",
      ""name"": ""Lemon herb salmon with roasted vegetables"",
      ""description"": ""Baked salmon, broccoli, sweet potato"",
      ""servings"": 2
    }
  ]
}");
        sb.AppendLine("mealType is one of Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, EveningSnack.");
        sb.AppendLine("Give every meal a real date on or after today. If they did not ask for a plan, write no JSON at all.");

        return sb.ToString();
    }

    /// <summary>A title for a new thread, taken from its opening question.</summary>
    public static string TitleFrom(string question)
    {
        var cleaned = question.Trim().ReplaceLineEndings(" ");
        if (cleaned.Length == 0)
        {
            return "New conversation";
        }

        return cleaned.Length <= 60 ? cleaned : cleaned[..57].TrimEnd() + "...";
    }

    /// <summary>
    /// The meals a reply offered, or an empty list. Anything dated before <paramref name="today"/>
    /// is dropped: a slot in the past cannot be cooked, and the calendar would not show it.
    /// </summary>
    public static List<PlannedMealDto> ParseSuggestions(string responseText, DateOnly today)
    {
        var meals = new List<PlannedMealDto>();

        var root = JsonExtraction.ReadObject(responseText);
        if (root == null
            || !root.Value.TryGetProperty("plannedMeals", out var planned)
            || planned.ValueKind != JsonValueKind.Array)
        {
            return meals;
        }

        foreach (var element in planned.EnumerateArray())
        {
            var name = JsonExtraction.ReadString(element, "name");
            if (name.Length == 0)
            {
                continue;
            }

            if (!DateOnly.TryParse(JsonExtraction.ReadString(element, "date"), out var date) || date < today)
            {
                continue;
            }

            var description = JsonExtraction.ReadString(element, "description");
            var servings = JsonExtraction.ReadFloat(element, "servings", 1f);

            meals.Add(new PlannedMealDto
            {
                Date = date,
                MealType = element.TryGetProperty("mealType", out var mealType)
                    ? MealTypeParsing.Read(mealType)
                    : MealType.Dinner,
                Name = name,
                Description = description.Length > 0 ? description : null,
                Servings = servings > 0 ? servings : 1f
            });
        }

        return meals;
    }

    private static void AppendProfile(StringBuilder sb, UserProfileDto? profile)
    {
        if (profile == null)
        {
            sb.AppendLine("You have no profile for this person yet, so keep advice general and say what you would need to know to be specific.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("About them:");
        sb.AppendLine($"- Age {profile.Age}, {profile.Gender}, {profile.WeightKg}kg, {profile.HeightCm}cm");
        sb.AppendLine($"- Goal: {profile.DietaryGoal}, activity: {profile.ActivityLevel}");
        sb.AppendLine($"- Daily targets: {profile.DailyCalorieTarget} kcal, {profile.DailyProteinTargetG}g protein, {profile.DailyCarbTargetG}g carbs, {profile.DailyFatTargetG}g fat");

        if (!string.IsNullOrWhiteSpace(profile.Notes))
        {
            sb.AppendLine($"- Notes: {profile.Notes}");
        }

        var exclusions = AINarration.ExclusionLine(profile.ExcludedFoods);
        if (exclusions.Length > 0)
        {
            sb.AppendLine($"- {exclusions}");
        }

        sb.AppendLine();
    }

    private static void AppendHistory(StringBuilder sb, IReadOnlyList<ChatMessage> history)
    {
        if (history.Count == 0)
        {
            return;
        }

        sb.AppendLine("The conversation so far:");
        foreach (var message in history)
        {
            var speaker = message.Role == ChatRole.User ? "Them" : "You";
            sb.AppendLine($"{speaker}: {message.Content}");
        }

        sb.AppendLine();
    }
}
