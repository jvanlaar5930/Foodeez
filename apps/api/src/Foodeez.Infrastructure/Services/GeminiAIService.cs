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

public class GeminiAIService : IAIService, IStreamingAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAIService> _logger;
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    // Google retires these on their own schedule - gemini-1.5-flash and then 2.5-flash both
    // started 404ing, taking every AI feature down with them - and an overloaded model 503s
    // for hours at a time. Keep it in configuration so the next one is a settings change
    // rather than a deploy.
    private const string DefaultModel = "gemini-3.6-flash";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetApiKey() =>
        _configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");

    private string GetModel() => _configuration["Gemini:Model"] ?? DefaultModel;

    private async Task<string> SendAsync(string prompt, CancellationToken ct)
    {
        var url = $"{ApiBase}/{GetModel()}:generateContent?key={GetApiKey()}";
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = 8192, temperature = 0.3 }
        };

        var json = JsonSerializer.Serialize(body, JsonOptions);
        var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);

        // EnsureSuccessStatusCode discards the body, and the body is where Google explains
        // itself - "model is overloaded", "no longer available, use X". Without it a caller
        // sees a bare 503 and cannot tell a temporary spike from a retired model.
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            if (error.Length > 500) error = error[..500] + "...";
            throw new HttpRequestException(
                $"Gemini returned {(int)response.StatusCode} for model '{GetModel()}': {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
    {
        var prompt = BuildRecommendationsPrompt(profile, recentNutrition);
        try
        {
            var text = await SendAsync(prompt, ct);
            return ParseRecommendations(text, profile);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini: failed to get dietary recommendations.");
            return new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] };
        }
    }

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default)
    {
        var prompt = BuildMealPlanPrompt(request, profile);
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
            _logger.LogError(ex, "Gemini: failed to generate meal plan.");
            return new GeneratedMealPlanDto();
        }
    }

    public async Task<ParsedMealDto> ParseMealImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        try
        {
            var apiKey = GetApiKey();
            var url = $"{ApiBase}/{GetModel()}:generateContent?key={apiKey}";
            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new { inlineData = new { mimeType = mimeType ?? "image/jpeg", data = Convert.ToBase64String(imageData) } },
                            new { text = MealParsePrompt.BuildImage() }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body, JsonOptions);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            return MealParsePrompt.Parse(text);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini: failed to read a meal from a photo.");
            return ParsedMealDto.Unreadable;
        }
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
            _logger.LogError(ex, "Gemini: failed to read a described meal.");
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

            _logger.LogWarning("Gemini: returned no usable meal analysis.");
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini: failed to analyze meal.");
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

            _logger.LogWarning("Gemini: returned no usable analysis for {Date}.", request.Date);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini: failed to analyze the day.");
        }

        // An empty analysis is how a provider outage reaches the caller; nothing is stored for it.
        return new DayAnalysisDto();
    }

    private static string BuildRecommendationsPrompt(UserProfileDto p, DailyNutritionDto? n)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional nutritionist.");
        sb.AppendLine($"User: Age {p.Age}, {p.Gender}, {p.HeightCm}cm, {p.WeightKg}kg, Goal: {p.DietaryGoal}, Activity: {p.ActivityLevel}");
        sb.AppendLine($"Targets: {p.DailyCalorieTarget} kcal, {p.DailyProteinTargetG}g protein, {p.DailyCarbTargetG}g carbs, {p.DailyFatTargetG}g fat");
        if (n != null)
            sb.AppendLine($"Recent intake: {n.TotalCalories} kcal, {n.TotalProtein}g protein, {n.TotalCarbs}g carbs, {n.TotalFat}g fat");
        sb.AppendLine("Respond ONLY with JSON: {\"overallScore\":75,\"suggestions\":[],\"deficiencies\":[],\"tips\":[]}");
        return sb.ToString();
    }

    private static string BuildMealPlanPrompt(GenerateMealPlanRequest r, UserProfileDto p)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Generate a meal plan.");
        sb.AppendLine($"User: {p.WeightKg}kg, {p.DailyCalorieTarget} kcal/day, Goal: {p.DietaryGoal}");
        sb.AppendLine($"Period: {r.StartDate:yyyy-MM-dd} to {r.EndDate:yyyy-MM-dd}");
        sb.AppendLine("Cover every date in the period. Give each day a Breakfast, a Lunch and a Dinner at minimum. mealType must be one of: Breakfast, MorningSnack, Lunch, AfternoonSnack, Dinner, EveningSnack. recipeName is required and is what the user sees, so name a real dish.");
        sb.AppendLine("Respond ONLY with JSON: {\"days\":[{\"date\":\"2025-01-01\",\"meals\":[{\"mealType\":\"Breakfast\",\"recipeName\":\"\",\"recipeDescription\":\"\",\"instructions\":\"\",\"prepTimeMinutes\":10,\"cookTimeMinutes\":20,\"servings\":1,\"estimatedCalories\":400,\"estimatedProteinG\":20,\"estimatedCarbsG\":50,\"estimatedFatG\":15,\"tags\":[],\"ingredients\":[{\"name\":\"\",\"quantity\":100,\"unit\":\"g\"}]}]}]}");
        return sb.ToString();
    }



    private static DietaryRecommendationsDto ParseRecommendations(string text, UserProfileDto p)
    {
        try
        {
            var s = text.IndexOf('{'); var e = text.LastIndexOf('}');
            if (s < 0 || e < 0) return new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] };
            using var doc = JsonDocument.Parse(text[s..(e + 1)]);
            var r = doc.RootElement;
            return new DietaryRecommendationsDto
            {
                OverallScore = r.TryGetProperty("overallScore", out var sc) ? sc.GetInt32() : 50,
                Suggestions = r.TryGetProperty("suggestions", out var sg) ? sg.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [],
                Deficiencies = r.TryGetProperty("deficiencies", out var df) ? df.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : [],
                Tips = r.TryGetProperty("tips", out var tp) ? tp.EnumerateArray().Select(x => x.GetString() ?? "").ToList() : []
            };
        }
        catch { return new DietaryRecommendationsDto { OverallScore = 50, Suggestions = [], Deficiencies = [], Tips = [] }; }
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
                        if (m.TryGetProperty("recipeDescription", out var rd)) meal.RecipeDescription = rd.GetString();
                        if (m.TryGetProperty("servings", out var sv) && sv.TryGetInt32(out var svi)) meal.Servings = svi;
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



    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(NutritionEstimation.BuildPrompt(request), ct);
            var estimate = NutritionEstimation.Parse(responseText);
            if (estimate != null) return estimate;

            _logger.LogWarning("Gemini returned no usable nutrition estimate for '{Dish}'.", request.Name);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate nutrition with Gemini AI.");
        }

        return new EstimatedNutritionDto
            {
                Succeeded = false,
                Confidence = "low",
                Assumptions = "We could not estimate this one automatically. Enter the values you know.",
            };
    }

    /// <summary>Google's streaming endpoint, asked for as server-sent events rather than a JSON array.</summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = $"{ApiBase}/{GetModel()}:streamGenerateContent?alt=sse&key={GetApiKey()}";
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new { maxOutputTokens = 8192, temperature = 0.3 }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        // The body is where Google explains itself - an overloaded model, a retired one - and
        // a bare status code cannot tell those apart.
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            if (error.Length > 500) error = error[..500] + "...";
            throw new HttpRequestException(
                $"Gemini returned {(int)response.StatusCode} for model '{GetModel()}': {error}");
        }

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, ct))
        {
            var text = StreamingHttp.Read(payload, root =>
                root.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var chunk)
                    ? chunk.GetString()
                    : null);

            if (text.Length > 0) yield return text;
        }
    }

}
