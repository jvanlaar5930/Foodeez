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

// Local Ollama — runs on the same machine, completely free
public class OllamaAIService : IAIService, IStreamingAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaAIService> _logger;
    private const string DefaultBaseUrl = "http://localhost:11434";
    private const string DefaultModel = "llama3";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? DefaultBaseUrl;
        var model = _configuration["Ollama:Model"] ?? DefaultModel;

        var body = new
        {
            model,
            stream = false,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var response = await _httpClient.PostAsync(
            $"{baseUrl}/api/chat",
            new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
    {
        var prompt = $"You are a nutritionist. User: {profile.Age}yo {profile.Gender}, {profile.WeightKg}kg, Goal: {profile.DietaryGoal}. " +
                     "Respond ONLY with JSON: {\"overallScore\":75,\"suggestions\":[],\"deficiencies\":[],\"tips\":[]}";
        try
        {
            var text = await SendAsync(prompt, ct);
            return ExtractJson(text, r => new DietaryRecommendationsDto
            {
                OverallScore = GetInt(r, "overallScore", 50),
                Suggestions = GetList(r, "suggestions"),
                Deficiencies = GetList(r, "deficiencies"),
                Tips = GetList(r, "tips")
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
            _logger.LogError(ex, "Ollama: failed to get dietary recommendations.");
            return new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] };
        }
    }

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default)
    {
        var prompt = $"Generate a {(request.EndDate.DayNumber - request.StartDate.DayNumber + 1)}-day meal plan at {profile.DailyCalorieTarget} kcal/day. " +
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
            _logger.LogError(ex, "Ollama: failed to generate meal plan.");
            return new GeneratedMealPlanDto();
        }
    }

    public Task<ParsedMealDto> ParseMealImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        // No items and a reason, not a placeholder food: a made-up "Unknown Food" would be
        // logged and counted as though someone had really eaten it.
        _logger.LogWarning("Ollama: asked to read a meal from a photo, which it cannot do.");
        return Task.FromResult(new ParsedMealDto
        {
            Note = "Photos need a provider that can see - this Ollama model is text-only. Describe the meal instead."
        });
    }

    public async Task<ParsedMealDto> ParseMealDescriptionAsync(string description, CancellationToken ct = default)
    {
        try
        {
            return MealParsePrompt.Parse(await SendAsync(MealParsePrompt.BuildText(description), ct));
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama: failed to read a described meal.");
            return ParsedMealDto.Unreadable;
        }
    }

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(MealAnalysisPrompt.Build(request), ct);
            var analysis = MealAnalysisPrompt.Parse(responseText);
            if (analysis != null) return analysis;

            _logger.LogWarning("Ollama: returned no usable meal analysis.");
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama: failed to analyze meal.");
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

            _logger.LogWarning("Ollama: returned no usable analysis for {Date}.", request.Date);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama: failed to analyze the day.");
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
                        day.Meals.Add(meal);
                    }
                result.Days.Add(day);
            }
            return result;
        }
        catch { return new GeneratedMealPlanDto(); }
    }

    private static T? ExtractJson<T>(string text, Func<JsonElement, T> mapper)
    {
        var s = text.IndexOf('{'); var e = text.LastIndexOf('}');
        if (s < 0 || e < 0) return default;
        using var doc = JsonDocument.Parse(text[s..(e + 1)]);
        return mapper(doc.RootElement);
    }

    private static int GetInt(JsonElement r, string k, int fallback) => r.TryGetProperty(k, out var v) ? v.GetInt32() : fallback;
    private static string GetStr(JsonElement r, string k) => r.TryGetProperty(k, out var v) ? v.GetString() ?? "" : "";
    private static List<string> GetList(JsonElement r, string k) =>
        r.TryGetProperty(k, out var v) ? v.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList() : [];

    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(NutritionEstimation.BuildPrompt(request), ct);
            var estimate = NutritionEstimation.Parse(responseText);
            if (estimate != null) return estimate;

            _logger.LogWarning("Ollama returned no usable nutrition estimate for '{Dish}'.", request.Name);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate nutrition with Ollama AI.");
        }

        return new EstimatedNutritionDto
            {
                Succeeded = false,
                Confidence = "low",
                Assumptions = "We could not estimate this one automatically. Enter the values you know.",
            };
    }

    /// <summary>Ollama streams bare JSON objects, one per line, rather than server-sent events.</summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var baseUrl = _configuration["Ollama:BaseUrl"] ?? DefaultBaseUrl;
        var model = _configuration["Ollama:Model"] ?? DefaultModel;

        var body = new
        {
            model,
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/chat")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await foreach (var line in StreamingHttp.ReadJsonLinesAsync(response, ct))
        {
            var text = StreamingHttp.Read(line, root =>
                root.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

}
