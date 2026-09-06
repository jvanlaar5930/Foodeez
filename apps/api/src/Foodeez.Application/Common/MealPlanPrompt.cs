using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for AI meal plan generation, written for the streaming
/// path: it leads with prose so there is something to watch while a week of meals is written,
/// then the JSON the plan is built from.
/// </summary>
public static class MealPlanPrompt
{
    /// <summary>
    /// What the numbers in "mealType" mean.
    ///
    /// The example JSON below shows <c>"mealType": 1</c> and nothing anywhere said what 1 was,
    /// so the model had to guess the numbering from a single example - which is exactly what it
    /// did, putting dinners in the breakfast slot and snacks wherever. A wrong guess was then
    /// invisible: <see cref="MealTypeParsing"/> accepts any number the enum defines, so a
    /// confidently misnumbered plan parses perfectly and lands in the wrong rows.
    ///
    /// Built from the enum rather than typed out, so a new meal type cannot leave this
    /// describing a numbering that no longer exists.
    /// </summary>
    internal static string MealTypeLegend()
    {
        var sb = new StringBuilder();
        sb.AppendLine("\"mealType\" is one of these numbers. Use the number, and put each meal in the slot it belongs in:");

        foreach (var value in Enum.GetValues<MealType>())
        {
            sb.AppendLine($"  {(int)value} = {Spaced(value.ToString())}");
        }

        return sb.ToString();
    }

    /// <summary>"MorningSnack" reads as "Morning Snack", which is what the slot is called on screen.</summary>
    private static string Spaced(string pascalCase)
    {
        var sb = new StringBuilder();
        foreach (var c in pascalCase)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append(' ');
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// One day of the plan, which is how a plan is actually generated.
    ///
    /// A week used to be a single prompt: one request, one answer, one parse. That made every
    /// day depend on all the others surviving - a model that lost the thread on Thursday, or a
    /// response truncated at the token ceiling, threw away Monday through Wednesday as well,
    /// and left nothing to say which day had gone wrong. A day at a time is a shorter prompt
    /// and a much shorter answer, both of which a small local model handles far better, and
    /// each one is saved before the next is asked for.
    /// </summary>
    /// <param name="date">The single date being planned.</param>
    /// <param name="occupied">
    /// Slots on this date that already have something in them. These are shown to the model as
    /// fixed points and are never overwritten - they are usually meals somebody put there
    /// deliberately, and a generator that quietly replaced them would be a data-loss bug.
    /// </param>
    /// <param name="alreadyPlanned">
    /// What earlier days of this same run ended up with, so the model can avoid repeating
    /// itself. Without it every day is planned in ignorance of the others and the same two
    /// dinners tend to come back all week.
    /// </param>
    public static string BuildDay(
        GenerateMealPlanRequest request,
        UserProfileDto profile,
        DateOnly date,
        IReadOnlyCollection<string> occupied,
        IReadOnlyCollection<string> alreadyPlanned)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional meal planning assistant. Plan the meals for ONE day.");
        sb.AppendLine();
        sb.AppendLine($"The day to plan: {date:yyyy-MM-dd} ({date.DayOfWeek}).");
        sb.AppendLine();

        AppendProfile(sb, request, profile);

        if (occupied.Count > 0)
        {
            sb.AppendLine("Already planned for this day, and staying exactly as it is:");
            foreach (var line in occupied)
            {
                sb.AppendLine($"- {line}");
            }

            sb.AppendLine();
            sb.AppendLine("Do not plan those meal types again. Plan only the ones missing, and choose them so " +
                          "the day's totals work alongside what is already there.");
            sb.AppendLine();
        }

        if (alreadyPlanned.Count > 0)
        {
            sb.AppendLine("Already planned earlier in this same run, on other days:");
            foreach (var line in alreadyPlanned)
            {
                sb.AppendLine($"- {line}");
            }

            sb.AppendLine();
            sb.AppendLine("Vary from these unless the request below asks for repetition.");
            sb.AppendLine();
        }

        AppendPreviousPeriod(sb, request);
        AppendGuidance(sb, request);

        sb.AppendLine("Keep this one day close to the calorie and macro targets above.");
        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine("In those sentences, say what this day looks like and why - one day, not the whole week.");
        sb.AppendLine();
        sb.AppendLine(MealTypeLegend());
        sb.AppendLine("JSON shape - one day only, no \"days\" array:");
        sb.AppendLine(@"{
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
}");

        return sb.ToString();
    }

    /// <summary>Reads one day's meals back. No meals means nothing usable came out.</summary>
    public static GeneratedDayDto ParseDay(string responseText, DateOnly date)
    {
        var day = new GeneratedDayDto { Date = date };

        var root = JsonExtraction.ReadObject(responseText);
        if (root == null)
        {
            return day;
        }

        // "meals" is what BuildDay asks for. A model that fell back on the whole-plan shape -
        // wrapping the answer in "days" - is still answering the question, so that is read
        // too rather than thrown away for being the wrong shape.
        if (root.Value.TryGetProperty("meals", out var meals) && meals.ValueKind == JsonValueKind.Array)
        {
            foreach (var mealElement in meals.EnumerateArray())
            {
                day.Meals.Add(ReadMeal(mealElement));
            }

            return day;
        }

        if (root.Value.TryGetProperty("days", out var days) && days.ValueKind == JsonValueKind.Array)
        {
            foreach (var dayElement in days.EnumerateArray())
            {
                if (!dayElement.TryGetProperty("meals", out var nested) || nested.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var mealElement in nested.EnumerateArray())
                {
                    day.Meals.Add(ReadMeal(mealElement));
                }
            }
        }

        return day;
    }

    private static void AppendProfile(StringBuilder sb, GenerateMealPlanRequest request, UserProfileDto profile)
    {
        sb.AppendLine("User Profile:");
        sb.AppendLine($"- Age: {profile.Age}, Gender: {profile.Gender}");
        sb.AppendLine($"- Weight: {profile.WeightKg}kg, Daily Calories: {profile.DailyCalorieTarget}");
        sb.AppendLine($"- Protein: {profile.DailyProteinTargetG}g, Carbs: {profile.DailyCarbTargetG}g, Fat: {profile.DailyFatTargetG}g");
        sb.AppendLine($"- Goal: {profile.DietaryGoal}, Activity: {profile.ActivityLevel}");

        if (request.PreferenceTags.Any())
            sb.AppendLine($"- Dietary Preferences: {string.Join(", ", request.PreferenceTags)}");
        if (request.ExcludeIngredients.Any())
            sb.AppendLine($"- Exclude Ingredients: {string.Join(", ", request.ExcludeIngredients)}");

        sb.AppendLine();
    }

    private static void AppendPreviousPeriod(StringBuilder sb, GenerateMealPlanRequest request)
    {
        if (request.PreviousPeriod.Count == 0)
        {
            return;
        }

        sb.AppendLine("What they had planned for the period before this one:");
        foreach (var line in request.PreviousPeriod)
        {
            sb.AppendLine($"- {line}");
        }

        sb.AppendLine();
        sb.AppendLine("Use it only as far as their request below asks you to - repeating it wholesale is not the default.");
        sb.AppendLine();
    }

    private static void AppendGuidance(StringBuilder sb, GenerateMealPlanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Guidance))
        {
            return;
        }

        sb.AppendLine("What they asked for this time, in their own words:");
        sb.AppendLine(request.Guidance.Trim());
        sb.AppendLine();
        // A free-text box must not become a way around an allergy. The instruction can
        // shape the plan; it cannot put back something the profile rules out.
        sb.AppendLine("Follow that request wherever it does not conflict with the targets or the excluded foods above - those still hold, whatever it says.");
        sb.AppendLine();
    }

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

        if (request.PreviousPeriod.Count > 0)
        {
            sb.AppendLine("What they had planned for the period before this one:");
            foreach (var line in request.PreviousPeriod)
            {
                sb.AppendLine($"- {line}");
            }

            sb.AppendLine();
            sb.AppendLine("Use it only as far as their request below asks you to - repeating it wholesale is not the default.");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(request.Guidance))
        {
            sb.AppendLine("What they asked for this time, in their own words:");
            sb.AppendLine(request.Guidance.Trim());
            sb.AppendLine();
            // A free-text box must not become a way around an allergy. The instruction can
            // shape the plan; it cannot put back something the profile rules out.
            sb.AppendLine("Follow that request wherever it does not conflict with the targets or the excluded foods above - those still hold, whatever it says.");
            sb.AppendLine();
        }

        sb.AppendLine("Cover every date in the period, and keep each day close to the calorie and macro targets.");
        sb.AppendLine();
        sb.AppendLine(AINarration.Instruction);
        sb.AppendLine("In those sentences, say what the week looks like overall - the sort of food, how it meets the goal - not a list of every meal.");
        sb.AppendLine();
        sb.AppendLine(MealTypeLegend());
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
