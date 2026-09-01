using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for AI meal plan generation, written for the streaming
/// path: it leads with prose so there is something to watch while a week of meals is written,
/// then the JSON the plan is built from.
/// </summary>
public static class MealPlanPrompt
{
    public static string Build(GenerateMealPlanRequest request, UserProfileDto profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional meal planning assistant. Generate a detailed meal plan based on the user's profile and preferences.");
        sb.AppendLine();
        sb.AppendLine("User Profile:");
        sb.AppendLine($"- Age: {profile.Age}, Gender: {profile.Gender}");
        sb.AppendLine($"- Weight: {profile.WeightKg}kg, Daily Calories: {profile.DailyCalorieTarget}");
        sb.AppendLine($"- Protein: {profile.DailyProteinTargetG}g, Carbs: {profile.DailyCarbTargetG}g, Fat: {profile.DailyFatTargetG}g");
        sb.AppendLine($"- Goal: {profile.DietaryGoal}, Activity: {profile.ActivityLevel}");

        if (request.PreferenceTags.Any())
            sb.AppendLine($"- Dietary Preferences: {string.Join(", ", request.PreferenceTags)}");
        if (request.ExcludeIngredients.Any())
            sb.AppendLine($"- Exclude Ingredients: {string.Join(", ", request.ExcludeIngredients)}");

        sb.AppendLine($"- Plan Period: {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}");
        sb.AppendLine();
        sb.AppendLine("Cover every date in the period, and keep each day close to the calorie and macro targets.");
        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine("In those sentences, say what the week looks like overall - the sort of food, how it meets the goal - not a list of every meal.");
        sb.AppendLine();
        sb.AppendLine("JSON shape:");
        sb.AppendLine(@"{
  ""days"": [
    {
      ""date"": ""2025-01-01"",
      ""meals"": [
        {
          ""mealType"": 1,
          ""recipeName"": ""Oatmeal with Berries"",
          ""recipeDescription"": ""Healthy breakfast"",
          ""instructions"": ""Cook oatmeal per package instructions, top with fresh berries."",
          ""prepTimeMinutes"": 5,
          ""cookTimeMinutes"": 10,
          ""servings"": 1,
          ""estimatedCalories"": 350,
          ""estimatedProteinG"": 12,
          ""estimatedCarbsG"": 55,
          ""estimatedFatG"": 8,
          ""tags"": [""breakfast"", ""healthy""],
          ""ingredients"": [
            { ""name"": ""Rolled oats"", ""quantity"": 80, ""unit"": ""g"" },
            { ""name"": ""Mixed berries"", ""quantity"": 100, ""unit"": ""g"" }
          ]
        }
      ]
    }
  ]
}");

        return sb.ToString();
    }

    /// <summary>Reads the model's plan back. A plan with no days means nothing usable came out.</summary>
    public static GeneratedMealPlanDto Parse(string responseText)
    {
        var plan = new GeneratedMealPlanDto();

        var root = JsonExtraction.ReadObject(responseText);
        if (root == null || !root.Value.TryGetProperty("days", out var days) || days.ValueKind != JsonValueKind.Array)
        {
            return plan;
        }

        foreach (var dayElement in days.EnumerateArray())
        {
            var day = new GeneratedDayDto();

            if (dayElement.TryGetProperty("date", out var date) && DateOnly.TryParse(date.GetString(), out var parsedDate))
            {
                day.Date = parsedDate;
            }

            if (dayElement.TryGetProperty("meals", out var meals) && meals.ValueKind == JsonValueKind.Array)
            {
                foreach (var mealElement in meals.EnumerateArray())
                {
                    day.Meals.Add(ReadMeal(mealElement));
                }
            }

            plan.Days.Add(day);
        }

        return plan;
    }

    private static GeneratedMealEntryDto ReadMeal(JsonElement element)
    {
        var meal = new GeneratedMealEntryDto
        {
            RecipeName = JsonExtraction.ReadString(element, "recipeName"),
            Instructions = JsonExtraction.ReadString(element, "instructions"),
            PrepTimeMinutes = JsonExtraction.ReadInt(element, "prepTimeMinutes"),
            CookTimeMinutes = JsonExtraction.ReadInt(element, "cookTimeMinutes"),
            Servings = JsonExtraction.ReadInt(element, "servings", 1),
            EstimatedCalories = JsonExtraction.ReadFloat(element, "estimatedCalories"),
            EstimatedProteinG = JsonExtraction.ReadFloat(element, "estimatedProteinG"),
            EstimatedCarbsG = JsonExtraction.ReadFloat(element, "estimatedCarbsG"),
            EstimatedFatG = JsonExtraction.ReadFloat(element, "estimatedFatG"),
            Tags = JsonExtraction.ReadStrings(element, "tags")
        };

        if (element.TryGetProperty("mealType", out var mealType))
        {
            meal.MealType = MealTypeParsing.Read(mealType);
        }

        var description = JsonExtraction.ReadString(element, "recipeDescription");
        meal.RecipeDescription = description.Length > 0 ? description : null;

        if (element.TryGetProperty("ingredients", out var ingredients) && ingredients.ValueKind == JsonValueKind.Array)
        {
            foreach (var ingredient in ingredients.EnumerateArray())
            {
                var notes = JsonExtraction.ReadString(ingredient, "notes");

                meal.Ingredients.Add(new GeneratedIngredientDto
                {
                    Name = JsonExtraction.ReadString(ingredient, "name"),
                    Quantity = JsonExtraction.ReadFloat(ingredient, "quantity"),
                    Unit = JsonExtraction.ReadString(ingredient, "unit"),
                    Notes = notes.Length > 0 ? notes : null
                });
            }
        }

        return meal;
    }
}
