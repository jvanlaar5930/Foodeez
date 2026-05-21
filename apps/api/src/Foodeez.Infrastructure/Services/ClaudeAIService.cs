using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public class ClaudeAIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeAIService> _logger;
    private const string AnthropicBaseUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeAIService(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude:ApiKey configuration is required.");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
    }

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null)
    {
        var prompt = BuildRecommendationsPrompt(profile, recentNutrition);

        try
        {
            var responseText = await SendMessageAsync(prompt);
            return ParseRecommendationsResponse(responseText, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dietary recommendations from Claude AI.");
            return BuildFallbackRecommendations(profile);
        }
    }

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile)
    {
        var prompt = BuildMealPlanPrompt(request, profile);

        try
        {
            var responseText = await SendMessageAsync(prompt);
            return ParseMealPlanResponse(responseText, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate meal plan from Claude AI.");
            return new GeneratedMealPlanDto();
        }
    }

    public async Task<ParsedFoodDto> ParseFoodImageAsync(byte[] imageData, string? mimeType = "image/jpeg")
    {
        var base64Image = Convert.ToBase64String(imageData);

        var requestBody = new
        {
            model = _configuration["Claude:Model"] ?? "claude-sonnet-4-6",
            max_tokens = 1024,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mimeType ?? "image/jpeg",
                                data = base64Image
                            }
                        },
                        new
                        {
                            type = "text",
                            text = BuildFoodImagePrompt()
                        }
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(AnthropicBaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseText = ExtractTextFromResponse(responseBody);
            return ParseFoodImageResponse(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse food image from Claude AI.");
            return new ParsedFoodDto
            {
                Name = "Unknown Food",
                ServingSize = 100,
                ServingUnit = "g",
                Confidence = 0f,
                NutritionalInfo = new NutritionalInfoDto()
            };
        }
    }

    // ────────────────────────── Private Helpers ──────────────────────────

    private async Task<string> SendMessageAsync(string prompt)
    {
        var model = _configuration["Claude:Model"] ?? "claude-sonnet-4-6";
        var requestBody = new
        {
            model,
            max_tokens = 2048,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(AnthropicBaseUrl, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        return ExtractTextFromResponse(responseBody);
    }

    private static string ExtractTextFromResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("content", out var contentArray) && contentArray.GetArrayLength() > 0)
        {
            var firstContent = contentArray[0];
            if (firstContent.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string BuildRecommendationsPrompt(UserProfileDto profile, DailyNutritionDto? recentNutrition)
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

    private static string BuildMealPlanPrompt(GenerateMealPlanRequest request, UserProfileDto profile)
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
        sb.AppendLine("Generate a meal plan. Respond ONLY with a valid JSON object (no markdown, no extra text) in this format:");
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

    private static string BuildFoodImagePrompt()
    {
        return @"Analyze this food image and identify the food item(s). Respond ONLY with a valid JSON object (no markdown, no extra text) in this format:
{
  ""name"": ""Food Name"",
  ""brand"": null,
  ""servingSize"": 100,
  ""servingUnit"": ""g"",
  ""confidence"": 0.85,
  ""nutritionalInfo"": {
    ""calories"": 250,
    ""protein"": 10,
    ""carbohydrates"": 30,
    ""fat"": 8,
    ""fiber"": 3,
    ""sugar"": 5,
    ""sodium"": 200
  }
}";
    }

    private static DietaryRecommendationsDto ParseRecommendationsResponse(string responseText, UserProfileDto profile)
    {
        try
        {
            // Extract JSON from response (handle potential extra text)
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0)
                return BuildFallbackRecommendations(profile);

            var jsonText = responseText[jsonStart..(jsonEnd + 1)];
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var dto = new DietaryRecommendationsDto();

            if (root.TryGetProperty("overallScore", out var score))
                dto.OverallScore = score.GetInt32();

            if (root.TryGetProperty("suggestions", out var suggestions))
                dto.Suggestions = suggestions.EnumerateArray().Select(e => e.GetString() ?? "").ToList();

            if (root.TryGetProperty("deficiencies", out var deficiencies))
                dto.Deficiencies = deficiencies.EnumerateArray().Select(e => e.GetString() ?? "").ToList();

            if (root.TryGetProperty("tips", out var tips))
                dto.Tips = tips.EnumerateArray().Select(e => e.GetString() ?? "").ToList();

            return dto;
        }
        catch
        {
            return BuildFallbackRecommendations(profile);
        }
    }

    private static DietaryRecommendationsDto BuildFallbackRecommendations(UserProfileDto profile)
    {
        return new DietaryRecommendationsDto
        {
            OverallScore = 50,
            Suggestions = new List<string>
            {
                $"Aim for {profile.DailyCalorieTarget} calories per day.",
                $"Target {profile.DailyProteinTargetG}g of protein daily."
            },
            Deficiencies = new List<string>(),
            Tips = new List<string>
            {
                "Stay hydrated by drinking 8 glasses of water per day.",
                "Include a variety of colorful vegetables in your meals."
            }
        };
    }

    private static GeneratedMealPlanDto ParseMealPlanResponse(string responseText, GenerateMealPlanRequest request)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0)
                return new GeneratedMealPlanDto();

            var jsonText = responseText[jsonStart..(jsonEnd + 1)];
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var result = new GeneratedMealPlanDto();

            if (!root.TryGetProperty("days", out var daysArray))
                return result;

            foreach (var dayElement in daysArray.EnumerateArray())
            {
                var day = new GeneratedDayDto();

                if (dayElement.TryGetProperty("date", out var dateEl) &&
                    DateOnly.TryParse(dateEl.GetString(), out var date))
                {
                    day.Date = date;
                }

                if (dayElement.TryGetProperty("meals", out var mealsArray))
                {
                    foreach (var mealElement in mealsArray.EnumerateArray())
                    {
                        var meal = new GeneratedMealEntryDto();

                        if (mealElement.TryGetProperty("mealType", out var mt))
                            meal.MealType = (MealType)mt.GetInt32();
                        if (mealElement.TryGetProperty("recipeName", out var rn))
                            meal.RecipeName = rn.GetString() ?? string.Empty;
                        if (mealElement.TryGetProperty("recipeDescription", out var rd))
                            meal.RecipeDescription = rd.GetString();
                        if (mealElement.TryGetProperty("instructions", out var instr))
                            meal.Instructions = instr.GetString() ?? string.Empty;
                        if (mealElement.TryGetProperty("prepTimeMinutes", out var prep))
                            meal.PrepTimeMinutes = prep.GetInt32();
                        if (mealElement.TryGetProperty("cookTimeMinutes", out var cook))
                            meal.CookTimeMinutes = cook.GetInt32();
                        if (mealElement.TryGetProperty("servings", out var servings))
                            meal.Servings = servings.GetInt32();
                        if (mealElement.TryGetProperty("estimatedCalories", out var cal))
                            meal.EstimatedCalories = cal.GetSingle();
                        if (mealElement.TryGetProperty("estimatedProteinG", out var prot))
                            meal.EstimatedProteinG = prot.GetSingle();
                        if (mealElement.TryGetProperty("estimatedCarbsG", out var carbs))
                            meal.EstimatedCarbsG = carbs.GetSingle();
                        if (mealElement.TryGetProperty("estimatedFatG", out var fat))
                            meal.EstimatedFatG = fat.GetSingle();

                        if (mealElement.TryGetProperty("tags", out var tags))
                            meal.Tags = tags.EnumerateArray().Select(t => t.GetString() ?? "").ToList();

                        if (mealElement.TryGetProperty("ingredients", out var ingreds))
                        {
                            foreach (var ingred in ingreds.EnumerateArray())
                            {
                                var ingredient = new GeneratedIngredientDto();
                                if (ingred.TryGetProperty("name", out var iname))
                                    ingredient.Name = iname.GetString() ?? string.Empty;
                                if (ingred.TryGetProperty("quantity", out var iqty))
                                    ingredient.Quantity = iqty.GetSingle();
                                if (ingred.TryGetProperty("unit", out var iunit))
                                    ingredient.Unit = iunit.GetString() ?? string.Empty;
                                if (ingred.TryGetProperty("notes", out var inotes))
                                    ingredient.Notes = inotes.GetString();
                                meal.Ingredients.Add(ingredient);
                            }
                        }

                        day.Meals.Add(meal);
                    }
                }

                result.Days.Add(day);
            }

            return result;
        }
        catch
        {
            return new GeneratedMealPlanDto();
        }
    }

    private static ParsedFoodDto ParseFoodImageResponse(string responseText)
    {
        try
        {
            var jsonStart = responseText.IndexOf('{');
            var jsonEnd = responseText.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0)
                return DefaultParsedFood();

            var jsonText = responseText[jsonStart..(jsonEnd + 1)];
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;

            var dto = new ParsedFoodDto();

            if (root.TryGetProperty("name", out var name))
                dto.Name = name.GetString() ?? "Unknown Food";
            if (root.TryGetProperty("brand", out var brand) && brand.ValueKind != JsonValueKind.Null)
                dto.Brand = brand.GetString();
            if (root.TryGetProperty("servingSize", out var ss))
                dto.ServingSize = ss.GetSingle();
            if (root.TryGetProperty("servingUnit", out var su))
                dto.ServingUnit = su.GetString() ?? "g";
            if (root.TryGetProperty("confidence", out var conf))
                dto.Confidence = conf.GetSingle();

            if (root.TryGetProperty("nutritionalInfo", out var ni))
            {
                dto.NutritionalInfo = new NutritionalInfoDto();
                if (ni.TryGetProperty("calories", out var cal)) dto.NutritionalInfo.Calories = cal.GetSingle();
                if (ni.TryGetProperty("protein", out var prot)) dto.NutritionalInfo.Protein = prot.GetSingle();
                if (ni.TryGetProperty("carbohydrates", out var carbs)) dto.NutritionalInfo.Carbohydrates = carbs.GetSingle();
                if (ni.TryGetProperty("fat", out var fat)) dto.NutritionalInfo.Fat = fat.GetSingle();
                if (ni.TryGetProperty("fiber", out var fiber)) dto.NutritionalInfo.Fiber = fiber.GetSingle();
                if (ni.TryGetProperty("sugar", out var sugar)) dto.NutritionalInfo.Sugar = sugar.GetSingle();
                if (ni.TryGetProperty("sodium", out var sodium)) dto.NutritionalInfo.Sodium = sodium.GetSingle();
            }

            return dto;
        }
        catch
        {
            return DefaultParsedFood();
        }
    }

    private static ParsedFoodDto DefaultParsedFood() => new ParsedFoodDto
    {
        Name = "Unknown Food",
        ServingSize = 100,
        ServingUnit = "g",
        Confidence = 0f,
        NutritionalInfo = new NutritionalInfoDto()
    };
}
