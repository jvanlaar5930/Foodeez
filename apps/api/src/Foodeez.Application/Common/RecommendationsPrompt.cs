using System.Text;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for dietary recommendations.
///
/// This was the last feature still building its prompt inside each provider, and the five
/// copies had drifted a long way apart: Claude's asked for the full profile and a week of
/// recent nutrition, while Groq's and Ollama's were a single interpolated line that omitted
/// the recent nutrition entirely - so the same user got noticeably worse advice depending on
/// which provider happened to be selected. None of them mentioned excluded foods, which meant
/// recommendations could suggest something the user is allergic to.
/// </summary>
public static class RecommendationsPrompt
{
    public static string Build(
        UserProfileDto profile,
        DailyNutritionDto? recentNutrition = null,
        IReadOnlyCollection<string>? excludedFoods = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional nutritionist. Analyze the following user profile and recent nutrition data, then provide personalized dietary recommendations.");
        sb.AppendLine();
        sb.AppendLine("User Profile:");
        sb.AppendLine($"- Age: {profile.Age}, Gender: {profile.Gender}");
        sb.AppendLine($"- Height: {profile.HeightCm} cm, Weight: {profile.WeightKg} kg");

        if (profile.TargetWeightKg.HasValue)
            sb.AppendLine($"- Target Weight: {profile.TargetWeightKg} kg");

        sb.AppendLine($"- Activity Level: {profile.ActivityLevel}");
        sb.AppendLine($"- Dietary Goal: {profile.DietaryGoal}");
        sb.AppendLine($"- Daily Calorie Target: {profile.DailyCalorieTarget} kcal");
        sb.AppendLine($"- Protein Target: {profile.DailyProteinTargetG}g, Carbs Target: {profile.DailyCarbTargetG}g, Fat Target: {profile.DailyFatTargetG}g");

        var exclusions = excludedFoods ?? profile.ExcludedFoods;
        if (exclusions is { Count: > 0 })
        {
            sb.AppendLine();
            sb.AppendLine(AINarration.ExclusionLine(exclusions));
        }

        if (recentNutrition != null)
        {
            sb.AppendLine();
            sb.AppendLine("Recent Average Daily Nutrition (last 7 days):");
            sb.AppendLine($"- Calories: {recentNutrition.TotalCalories} kcal (target: {recentNutrition.TargetCalories})");
            sb.AppendLine($"- Protein: {recentNutrition.TotalProtein}g (target: {recentNutrition.TargetProtein}g)");
            sb.AppendLine($"- Carbohydrates: {recentNutrition.TotalCarbs}g (target: {recentNutrition.TargetCarbs}g)");
            sb.AppendLine($"- Fat: {recentNutrition.TotalFat}g (target: {recentNutrition.TargetFat}g)");
            sb.AppendLine($"- Fiber: {recentNutrition.TotalFiber}g");
        }

        sb.AppendLine();
        sb.AppendLine("Respond ONLY with a valid JSON object in this exact structure (no markdown, no extra text):");
        sb.AppendLine(@"{
  ""overallScore"": 75,
  ""suggestions"": [""suggestion 1"", ""suggestion 2""],
  ""deficiencies"": [""nutrient deficiency 1""],
  ""tips"": [""practical tip 1"", ""practical tip 2""]
}");

        return sb.ToString();
    }

    /// <summary>The recommendations in a model's answer, or null when there are none to read.</summary>
    public static DietaryRecommendationsDto? Parse(string responseText)
    {
        var root = JsonExtraction.ReadObject(responseText);
        if (root == null)
        {
            return null;
        }

        return new DietaryRecommendationsDto
        {
            OverallScore = JsonExtraction.ReadInt(root.Value, "overallScore", DefaultScore),
            Suggestions = JsonExtraction.ReadStrings(root.Value, "suggestions"),
            Deficiencies = JsonExtraction.ReadStrings(root.Value, "deficiencies"),
            Tips = JsonExtraction.ReadStrings(root.Value, "tips")
        };
    }

    /// <summary>
    /// A neutral middle score. Used when a provider answered but without a score, and as the
    /// value in <see cref="Unavailable"/> - "we don't know" reads better as the middle of the
    /// range than as a zero the user would take for a damning verdict on their eating.
    /// </summary>
    private const int DefaultScore = 50;

    /// <summary>
    /// What a caller gets when the provider could not be reached at all.
    ///
    /// Deliberately not empty. Four of the five providers used to return a blank result here,
    /// so an outage showed the user a page with nothing on it; Claude alone built something
    /// from the profile it already had. That is the better answer - none of it needs a model -
    /// so it is now what everyone returns.
    /// </summary>
    public static DietaryRecommendationsDto Fallback(UserProfileDto profile) => new()
    {
        OverallScore = DefaultScore,
        Suggestions =
        [
            $"Aim for {profile.DailyCalorieTarget} calories per day.",
            $"Target {profile.DailyProteinTargetG}g of protein daily."
        ],
        Deficiencies = [],
        Tips =
        [
            "Stay hydrated by drinking 8 glasses of water per day.",
            "Include a variety of colorful vegetables in your meals."
        ]
    };
}
