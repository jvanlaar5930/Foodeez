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

// OpenAI-compatible API via Groq (free tier) — https://console.groq.com
public class GroqAIService : IAIService, IStreamingAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroqAIService> _logger;
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string DefaultModel = "llama-3.1-8b-instant";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GroqAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        var apiKey = _configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
        var model = _configuration["Groq:Model"] ?? DefaultModel;

        var body = new
        {
            model,
            max_tokens = 2048,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
    {
        var prompt = $"You are a nutritionist. User: Age {profile.Age}, {profile.Gender}, {profile.WeightKg}kg, Goal: {profile.DietaryGoal}, Activity: {profile.ActivityLevel}. " +
                     $"Targets: {profile.DailyCalorieTarget} kcal, {profile.DailyProteinTargetG}g protein. " +
                     "Respond ONLY with JSON: {\"overallScore\":75,\"suggestions\":[],\"deficiencies\":[],\"tips\":[]}";
        try
        {
            var text = await SendAsync(prompt, ct);
            return ParseJson<DietaryRecommendationsDto>(text, r => new DietaryRecommendationsDto
            {
                OverallScore = GetInt(r, "overallScore", 50),
                Suggestions = GetStringList(r, "suggestions"),
                Deficiencies = GetStringList(r, "deficiencies"),
                Tips = GetStringList(r, "tips")
            }) ?? new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] };
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq: failed to get dietary recommendations.");
            return new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] };
        }
    }

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default)
    {
        var prompt = $"Generate a meal plan for {profile.DailyCalorieTarget} kcal/day from {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}. " +
                     "Cover every date in the period. Give each day a Breakfast, a Lunch and a Dinner at minimum. mealType must be one of: Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, EveningSnack. recipeName is required and is what the user sees, so name a real dish. " +
                     "Respond ONLY with JSON: {\"days\":[{\"date\":\"2025-01-01\",\"meals\":[{\"mealType\":\"Breakfast\",\"recipeName\":\"\",\"recipeDescription\":\"\",\"instructions\":\"\",\"prepTimeMinutes\":10,\"cookTimeMinutes\":20,\"servings\":1,\"estimatedCalories\":400,\"estimatedProteinG\":20,\"estimatedCarbsG\":50,\"estimatedFatG\":15,\"tags\":[],\"ingredients\":[]}]}]}";
        try
        {
            var text = await SendAsync(prompt, ct);
            return ParseMealPlan(text);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq: failed to generate meal plan.");
            return new GeneratedMealPlanDto();
        }
    }

    public Task<ParsedFoodDto> ParseFoodImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        // Groq free tier does not support vision; return a placeholder
        _logger.LogWarning("Groq does not support image parsing. Returning default food item.");
        return Task.FromResult(new ParsedFoodDto
        {
            Name = "Unknown Food", ServingSize = 100, ServingUnit = "g", Confidence = 0f,
            NutritionalInfo = new NutritionalInfoDto()
        });
    }

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(MealAnalysisPrompt.Build(request), ct);
            var analysis = MealAnalysisPrompt.Parse(responseText);
            if (analysis != null) return analysis;

            _logger.LogWarning("Groq: returned no usable meal analysis.");
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq: failed to analyze meal.");
        }

        return new MealAnalysisDto { Score = 0, Completeness = "Analysis unavailable.", Missing = [], Suggestions = [] };
    }

    public async Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var text = await SendAsync(DayAnalysisPrompt.Build(request), ct);
            var analysis = DayAnalysisPrompt.Parse(text);
            if (analysis != null) return analysis;

            _logger.LogWarning("Groq: returned no usable analysis for {Date}.", request.Date);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq: failed to analyze the day.");
        }

        // An empty analysis is how a provider outage reaches the caller; nothing is stored for it.
        return new DayAnalysisDto();
    }

    private static GeneratedMealPlanDto ParseMealPlan(string text)
    {
        try
        {
            var s = text.IndexOf('{'); var e = text.LastIndexOf('}');
            if (s < 0 || e < 0) return new GeneratedMealPlanDto();
            using var doc = JsonDocument.Parse(text[s..(e + 1)]);
            var result = new GeneratedMealPlanDto();
            if (!doc.RootElement.TryGetProperty("days", out var days)) return result;
            foreach (var d in days.EnumerateArray())
            {
                var day = new GeneratedDayDto();
                if (d.TryGetProperty("date", out var dt) && DateOnly.TryParse(dt.GetString(), out var date)) day.Date = date;
                if (d.TryGetProperty("meals", out var meals))
                    foreach (var m in meals.EnumerateArray())
                    {
                        var meal = new GeneratedMealEntryDto();
                        if (m.TryGetProperty("mealType", out var mt)) meal.MealType = MealTypeParsing.Read(mt);
                        if (m.TryGetProperty("recipeName", out var rn)) meal.RecipeName = rn.GetString() ?? "";
                        if (m.TryGetProperty("estimatedCalories", out var c)) meal.EstimatedCalories = c.GetSingle();
                        if (m.TryGetProperty("estimatedProteinG", out var p)) meal.EstimatedProteinG = p.GetSingle();
                        if (m.TryGetProperty("estimatedCarbsG", out var carbs)) meal.EstimatedCarbsG = carbs.GetSingle();
                        if (m.TryGetProperty("estimatedFatG", out var f)) meal.EstimatedFatG = f.GetSingle();
                        day.Meals.Add(meal);
                    }
                result.Days.Add(day);
            }
            return result;
        }
        catch { return new GeneratedMealPlanDto(); }
    }

    private static T? ParseJson<T>(string text, Func<JsonElement, T> mapper)
    {
        var s = text.IndexOf('{'); var e = text.LastIndexOf('}');
        if (s < 0 || e < 0) return default;
        using var doc = JsonDocument.Parse(text[s..(e + 1)]);
        return mapper(doc.RootElement);
    }

    private static int GetInt(JsonElement r, string key, int fallback) =>
        r.TryGetProperty(key, out var v) ? v.GetInt32() : fallback;

    private static string GetString(JsonElement r, string key) =>
        r.TryGetProperty(key, out var v) ? v.GetString() ?? "" : "";

    private static List<string> GetStringList(JsonElement r, string key) =>
        r.TryGetProperty(key, out var v)
            ? v.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
            : [];

    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(NutritionEstimation.BuildPrompt(request), ct);
            var estimate = NutritionEstimation.Parse(responseText);
            if (estimate != null) return estimate;

            _logger.LogWarning("Groq returned no usable nutrition estimate for '{Dish}'.", request.Name);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate nutrition with Groq AI.");
        }

        return new EstimatedNutritionDto
            {
                Succeeded = false,
                Confidence = "low",
                Assumptions = "We could not estimate this one automatically. Enter the values you know.",
            };
    }

    /// <summary>Groq speaks the OpenAI streaming protocol, so the frames read the same way.</summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var apiKey = _configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is not configured.");
        var model = _configuration["Groq:Model"] ?? DefaultModel;

        var body = new
        {
            model,
            max_tokens = 2048,
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.OpenAiDelta(payload);
            if (text.Length > 0) yield return text;
        }
    }

}
