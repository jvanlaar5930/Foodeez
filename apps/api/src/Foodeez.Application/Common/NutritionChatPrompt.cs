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
        sb.AppendLine("Some replies end with a single JSON object, written on its own with nothing after it.");
        sb.AppendLine("It may carry either or both of these keys, and you write it only when the rule below says so:");
        sb.AppendLine(@"{
  ""plannedMeals"": [
    {
      ""date"": ""2025-01-06"",
      ""mealType"": ""Dinner"",
      ""name"": ""Lemon herb salmon with roasted vegetables"",
      ""description"": ""Baked salmon, broccoli, sweet potato"",
      ""servings"": 2
    }
  ],
  ""recipes"": [
    {
      ""name"": ""Lemon herb salmon with roasted vegetables"",
      ""description"": ""One tray, thirty minutes, and enough protein for a training day."",
      ""prepTimeMinutes"": 10,
      ""cookTimeMinutes"": 25,
      ""servings"": 2,
      ""tags"": ""dinner, high-protein, one-pan"",
      ""ingredients"": [
        { ""name"": ""Salmon fillet"", ""quantity"": 2, ""unit"": ""fillets"", ""notes"": ""skin on"" },
        { ""name"": ""Broccoli"", ""quantity"": 300, ""unit"": ""g"" }
      ],
      ""instructions"": ""1. Heat the oven to 200C.\n2. Toss the vegetables in oil and roast for 15 minutes.\n3. Add the salmon and roast for 12 minutes more."",
      ""calories"": 520, ""protein"": 42, ""carbohydrates"": 28, ""fat"": 26, ""fiber"": 7, ""sugar"": 6, ""sodium"": 380
    }
  ]
}");
        sb.AppendLine();
        sb.AppendLine("plannedMeals: write it only if they asked you to plan meals, or agreed to a plan you offered.");
        sb.AppendLine("mealType is one of Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, EveningSnack.");
        sb.AppendLine("Give every meal a real date on or after today.");
        sb.AppendLine();
        sb.AppendLine("recipes: write it only for a dish you have actually set out how to cook in this reply -");
        sb.AppendLine("full ingredient list and numbered method. Never for a dish you merely mentioned or named.");
        sb.AppendLine("Repeat the method there in full, since the reader keeps that copy and not your prose.");
        sb.AppendLine("Quantities are numbers and units are separate: 300 and \"g\", not \"300g\". Nutrition is per serving.");
        sb.AppendLine();
        sb.AppendLine("If neither rule applies, write no JSON at all.");

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

    /// <summary>
    /// The recipes a reply wrote out, or an empty list. A recipe with no ingredients is
    /// dropped: the model naming a dish in passing is not something anyone can cook from,
    /// and it would sit in the library as an empty shell.
    /// </summary>
    public static List<SuggestedRecipeDto> ParseRecipes(string responseText)
    {
        var recipes = new List<SuggestedRecipeDto>();

        var root = JsonExtraction.ReadObject(responseText);
        if (root == null
            || !root.Value.TryGetProperty("recipes", out var written)
            || written.ValueKind != JsonValueKind.Array)
        {
            return recipes;
        }

        foreach (var element in written.EnumerateArray())
        {
            var name = JsonExtraction.ReadString(element, "name");
            if (name.Length == 0)
            {
                continue;
            }

            var ingredients = ReadIngredients(element);
            if (ingredients.Count == 0)
            {
                continue;
            }

            var description = JsonExtraction.ReadString(element, "description");
            var tags = JsonExtraction.ReadString(element, "tags");
            var servings = JsonExtraction.ReadInt(element, "servings", 1);

            recipes.Add(new SuggestedRecipeDto
            {
                Name = name,
                Description = description.Length > 0 ? description : null,
                Instructions = JsonExtraction.ReadString(element, "instructions"),
                PrepTimeMinutes = Math.Max(0, JsonExtraction.ReadInt(element, "prepTimeMinutes")),
                CookTimeMinutes = Math.Max(0, JsonExtraction.ReadInt(element, "cookTimeMinutes")),
                Servings = servings > 0 ? servings : 1,
                Tags = tags.Length > 0 ? tags : null,
                Ingredients = ingredients,
                Calories = JsonExtraction.ReadFloat(element, "calories"),
                Protein = JsonExtraction.ReadFloat(element, "protein"),
                Carbohydrates = JsonExtraction.ReadFloat(element, "carbohydrates"),
                Fat = JsonExtraction.ReadFloat(element, "fat"),
                Fiber = JsonExtraction.ReadFloat(element, "fiber"),
                Sugar = JsonExtraction.ReadFloat(element, "sugar"),
                Sodium = JsonExtraction.ReadFloat(element, "sodium")
            });
        }

        return recipes;
    }

    private static List<SuggestedRecipeIngredientDto> ReadIngredients(JsonElement recipe)
    {
        var ingredients = new List<SuggestedRecipeIngredientDto>();

        if (!recipe.TryGetProperty("ingredients", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return ingredients;
        }

        foreach (var element in array.EnumerateArray())
        {
            // A plain string is a shape the model falls back to often enough to be worth
            // taking: it is still a usable line, just without a parsed quantity.
            if (element.ValueKind == JsonValueKind.String)
            {
                var line = element.GetString() ?? string.Empty;
                if (line.Trim().Length > 0)
                {
                    ingredients.Add(new SuggestedRecipeIngredientDto { Name = line.Trim() });
                }

                continue;
            }

            var name = JsonExtraction.ReadString(element, "name");
            if (name.Length == 0)
            {
                continue;
            }

            var notes = JsonExtraction.ReadString(element, "notes");

            ingredients.Add(new SuggestedRecipeIngredientDto
            {
                Name = name,
                Quantity = JsonExtraction.ReadFloat(element, "quantity"),
                Unit = JsonExtraction.ReadString(element, "unit"),
                Notes = notes.Length > 0 ? notes : null
            });
        }

        return ingredients;
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
