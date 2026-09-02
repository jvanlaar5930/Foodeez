using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

public class ClaudeAIService : IAIService, IStreamingAIService
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

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
    {
        var prompt = BuildRecommendationsPrompt(profile, recentNutrition);

        try
        {
            var responseText = await SendMessageAsync(prompt, ct);
            return ParseRecommendationsResponse(responseText, profile);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get dietary recommendations from Claude AI.");
            return BuildFallbackRecommendations(profile);
        }
    }

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default)
    {
        var prompt = BuildMealPlanPrompt(request, profile);

        try
        {
            var responseText = await SendMessageAsync(prompt, ct);
            return ParseMealPlanResponse(responseText, request);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate meal plan from Claude AI.");
            return new GeneratedMealPlanDto();
        }
    }

    public async Task<ParsedMealDto> ParseMealImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        var base64Image = Convert.ToBase64String(imageData);

        var requestBody = new
        {
            model = _configuration["Claude:Model"] ?? "claude-sonnet-4-6",
            // A plate of food can run to a dozen entries, and a truncated answer parses as a
            // meal with items missing rather than as the failure it is.
            max_tokens = 4096,
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
                            text = MealParsePrompt.BuildImage()
                        }
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(AnthropicBaseUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var responseText = ExtractTextFromResponse(responseBody);
            return MealParsePrompt.Parse(responseText);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude: failed to read a meal from a photo.");
            return ParsedMealDto.Unreadable;
        }
    }

    public async Task<ParsedMealDto> ParseMealDescriptionAsync(string description, CancellationToken ct = default)
    {
        try
        {
            return MealParsePrompt.Parse(await SendMessageAsync(MealParsePrompt.BuildText(description), ct));
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude: failed to read a described meal.");
            return ParsedMealDto.Unreadable;
        }
    }

    // ────────────────────────── Private Helpers ──────────────────────────

    private async Task<string> SendMessageAsync(string prompt, CancellationToken ct)
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
        var response = await _httpClient.PostAsync(AnthropicBaseUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
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
                            meal.MealType = MealTypeParsing.Read(mt);
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

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendMessageAsync(MealAnalysisPrompt.Build(request), ct);
            var analysis = MealAnalysisPrompt.Parse(responseText);
            if (analysis != null) return analysis;

            _logger.LogWarning("Claude: returned no usable meal analysis.");
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude: failed to analyze meal.");
        }

        return new MealAnalysisDto { Score = 0, Completeness = "Analysis unavailable.", Missing = [], Suggestions = [] };
    }

    public async Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var text = await SendMessageAsync(DayAnalysisPrompt.Build(request), ct);
            var analysis = DayAnalysisPrompt.Parse(text);
            if (analysis != null) return analysis;

            _logger.LogWarning("Claude: returned no usable analysis for {Date}.", request.Date);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude: failed to analyze the day.");
        }

        // An empty analysis is how a provider outage reaches the caller; nothing is stored for it.
        return new DayAnalysisDto();
    }





    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendMessageAsync(NutritionEstimation.BuildPrompt(request), ct);
            var estimate = NutritionEstimation.Parse(responseText);
            if (estimate != null) return estimate;

            _logger.LogWarning("Claude returned no usable nutrition estimate for '{Dish}'.", request.Name);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate nutrition with Claude AI.");
        }

        return new EstimatedNutritionDto
            {
                Succeeded = false,
                Confidence = "low",
                Assumptions = "We could not estimate this one automatically. Enter the values you know.",
            };
    }

    /// <summary>
    /// The same request as <see cref="SendMessageAsync"/> with `stream` set, so the answer
    /// arrives in the pieces Anthropic writes it in rather than in one block at the end.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var body = new
        {
            model = _configuration["Claude:Model"] ?? "claude-sonnet-4-6",
            max_tokens = 2048,
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicBaseUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.Read(payload, root =>
                root.TryGetProperty("type", out var type) &&
                type.GetString() == "content_block_delta" &&
                root.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

}
