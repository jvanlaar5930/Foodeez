using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

// Any self-hosted server that speaks the OpenAI chat-completions API: LM Studio, llama.cpp
// (llama-server), vLLM, LocalAI, Jan, KoboldCpp, text-generation-webui — and Ollama through its
// /v1 endpoint. They differ only in the port and the model name, so one client covers them all.
//
// Configuration comes from the admin settings table first (local.*), falling back to
// appsettings (LocalAI:*), so the server can be pointed somewhere else without a redeploy.
public class LocalAIService : IAIService, IStreamingAIService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IAppSettingRepository _settings;
    private readonly ILogger<LocalAIService> _logger;

    private const string DefaultBaseUrl = "http://localhost:1234/v1";   // LM Studio's default
    private const string DefaultModel = "local-model";
    private const int DefaultTimeoutSeconds = 300;
    private const int MaxTokens = 4096;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public LocalAIService(
        HttpClient httpClient,
        IConfiguration configuration,
        IAppSettingRepository settings,
        ILogger<LocalAIService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _settings = settings;
        _logger = logger;
    }

    // ────────────────────────── Configuration ──────────────────────────

    // Admin-editable settings win; appsettings is the fallback for a machine with no rows yet.
    private async Task<string?> ResolveAsync(string settingKey, string configKey)
    {
        var value = await _settings.GetValueAsync(settingKey);
        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();

        value = _configuration[configKey];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Accepts whatever shape the admin pasted — "http://localhost:8080",
    /// "http://localhost:1234/v1", or the full ".../v1/chat/completions" — and returns the
    /// chat-completions endpoint.
    /// </summary>
    internal static string BuildChatCompletionsUrl(string baseUrl)
    {
        var url = baseUrl.Trim().TrimEnd('/');

        if (url.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return $"{url}/chat/completions";

        return $"{url}/v1/chat/completions";
    }

    private async Task<bool> SupportsVisionAsync()
    {
        var raw = await ResolveAsync("local.supportsVision", "LocalAI:SupportsVision");
        return bool.TryParse(raw, out var enabled) && enabled;
    }

    private async Task<TimeSpan> TimeoutAsync()
    {
        var raw = await ResolveAsync("local.timeoutSeconds", "LocalAI:TimeoutSeconds");
        var seconds = int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : DefaultTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds);
    }

    // ────────────────────────── Transport ──────────────────────────

    private Task<string> SendAsync(string prompt, CancellationToken ct) =>
        SendAsync(new object[] { new { role = "user", content = prompt } }, ct);

    // A local server runs on CPU or a single GPU and can take minutes on a long prompt, so the
    // deadline is per-request rather than HttpClient's fixed 100 seconds.
    private async Task<string> SendAsync(object[] messages, CancellationToken ct)
    {
        var baseUrl = await ResolveAsync("local.baseUrl", "LocalAI:BaseUrl") ?? DefaultBaseUrl;
        var model = await ResolveAsync("local.model", "LocalAI:Model") ?? DefaultModel;
        var apiKey = await ResolveAsync("local.apiKey", "LocalAI:ApiKey");

        var body = new
        {
            model,
            max_tokens = MaxTokens,
            stream = false,
            messages
        };

        var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUrl(baseUrl))
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        // Most local servers ignore the key; some (vLLM, a proxied LM Studio) require one.
        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // Linked, not standalone: the timeout still applies, but the caller going away now
        // ends the request too. A local model can hold a GPU for minutes, and before this the
        // only way to stop one was to restart the server.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(await TimeoutAsync());

        var response = await _httpClient.SendAsync(request, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cts.Token);
            throw new HttpRequestException(
                $"Local LLM at {baseUrl} returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(error, 500)}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cts.Token);
        return ExtractContent(responseBody);
    }

    // Standard OpenAI shape; a few servers answer with "text" or a bare "content" instead.
    private static string ExtractContent(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;

            if (first.TryGetProperty("text", out var text))
                return text.GetString() ?? string.Empty;
        }

        if (root.TryGetProperty("content", out var bare))
            return bare.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    // ────────────────────────── IAIService ──────────────────────────

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
    {
        var prompt = $"You are a nutritionist. User: Age {profile.Age}, {profile.Gender}, {profile.WeightKg}kg, Goal: {profile.DietaryGoal}, Activity: {profile.ActivityLevel}. " +
                     $"Targets: {profile.DailyCalorieTarget} kcal, {profile.DailyProteinTargetG}g protein. " +
                     "Respond ONLY with JSON: {\"overallScore\":75,\"suggestions\":[],\"deficiencies\":[],\"tips\":[]}";
        try
        {
            var text = await SendAsync(prompt, ct);
            return ParseJson(text, r => new DietaryRecommendationsDto
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
            _logger.LogError(ex, "Local LLM: failed to get dietary recommendations.");
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
            _logger.LogError(ex, "Local LLM: failed to generate meal plan.");
            return new GeneratedMealPlanDto();
        }
    }

    public async Task<ParsedFoodDto> ParseFoodImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        var unknownFood = new ParsedFoodDto
        {
            Name = "Unknown Food", ServingSize = 100, ServingUnit = "g", Confidence = 0f,
            NutritionalInfo = new NutritionalInfoDto()
        };

        // Sending an image to a text-only model only buys a slow failure, so it is opt-in.
        if (!await SupportsVisionAsync())
        {
            _logger.LogWarning(
                "Local LLM: image parsing is off. Load a vision model (e.g. a Qwen2-VL or LLaVA build) and set local.supportsVision to true.");
            return unknownFood;
        }

        var dataUri = $"data:{mimeType ?? "image/jpeg"};base64,{Convert.ToBase64String(imageData)}";
        var messages = new object[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = dataUri } },
                    new { type = "text", text = BuildFoodImagePrompt() }
                }
            }
        };

        try
        {
            var text = await SendAsync(messages, ct);
            return ParseJson(text, r => new ParsedFoodDto
            {
                Name = GetString(r, "name") is { Length: > 0 } name ? name : "Unknown Food",
                Brand = GetString(r, "brand") is { Length: > 0 } brand ? brand : null,
                ServingSize = GetFloat(r, "servingSize", 100),
                ServingUnit = GetString(r, "servingUnit") is { Length: > 0 } unit ? unit : "g",
                Confidence = GetFloat(r, "confidence", 0f),
                NutritionalInfo = r.TryGetProperty("nutritionalInfo", out var n)
                    ? new NutritionalInfoDto
                    {
                        Calories = GetFloat(n, "calories", 0),
                        Protein = GetFloat(n, "protein", 0),
                        Carbohydrates = GetFloat(n, "carbohydrates", 0),
                        Fat = GetFloat(n, "fat", 0),
                        Fiber = GetFloat(n, "fiber", 0),
                        Sugar = GetFloat(n, "sugar", 0),
                        Sodium = GetFloat(n, "sodium", 0)
                    }
                    : new NutritionalInfoDto()
            }) ?? unknownFood;
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local LLM: failed to parse food image.");
            return unknownFood;
        }
    }

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(MealAnalysisPrompt.Build(request), ct);
            var analysis = MealAnalysisPrompt.Parse(responseText);
            if (analysis != null) return analysis;

            _logger.LogWarning("Local LLM: returned no usable meal analysis.");
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local LLM: failed to analyze meal.");
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

            _logger.LogWarning("Local LLM: returned no usable analysis for {Date}.", request.Date);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local LLM: failed to analyze the day.");
        }

        // An empty analysis is how a provider outage reaches the caller; nothing is stored for it.
        return new DayAnalysisDto();
    }

    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        try
        {
            var responseText = await SendAsync(NutritionEstimation.BuildPrompt(request), ct);
            var estimate = NutritionEstimation.Parse(responseText);
            if (estimate != null) return estimate;

            _logger.LogWarning("Local LLM returned no usable nutrition estimate for '{Dish}'.", request.Name);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged
            // as one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate nutrition with the local LLM.");
        }

        return new EstimatedNutritionDto
        {
            Succeeded = false,
            Confidence = "low",
            Assumptions = "We could not estimate this one automatically. Enter the values you know.",
        };
    }

    // ────────────────────────── Parsing helpers ──────────────────────────

    private static string BuildFoodImagePrompt() =>
        @"Analyze this food image and identify the food item(s). Respond ONLY with a valid JSON object (no markdown, no extra text) in this format:
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
                        var meal = new GeneratedMealEntryDto
                        {
                            MealType = m.TryGetProperty("mealType", out var mt) ? MealTypeParsing.Read(mt) : MealType.Breakfast,
                            RecipeName = GetString(m, "recipeName"),
                            RecipeDescription = GetString(m, "recipeDescription") is { Length: > 0 } desc ? desc : null,
                            Instructions = GetString(m, "instructions"),
                            PrepTimeMinutes = GetInt(m, "prepTimeMinutes", 0),
                            CookTimeMinutes = GetInt(m, "cookTimeMinutes", 0),
                            Servings = GetInt(m, "servings", 1),
                            EstimatedCalories = GetFloat(m, "estimatedCalories", 0),
                            EstimatedProteinG = GetFloat(m, "estimatedProteinG", 0),
                            EstimatedCarbsG = GetFloat(m, "estimatedCarbsG", 0),
                            EstimatedFatG = GetFloat(m, "estimatedFatG", 0),
                            Tags = GetStringList(m, "tags")
                        };

                        if (m.TryGetProperty("ingredients", out var ingredients) && ingredients.ValueKind == JsonValueKind.Array)
                            foreach (var ing in ingredients.EnumerateArray())
                            {
                                if (ing.ValueKind != JsonValueKind.Object) continue;
                                meal.Ingredients.Add(new GeneratedIngredientDto
                                {
                                    Name = GetString(ing, "name"),
                                    Quantity = GetFloat(ing, "quantity", 0),
                                    Unit = GetString(ing, "unit"),
                                    Notes = GetString(ing, "notes") is { Length: > 0 } notes ? notes : null
                                });
                            }

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

    // Small local models are loose with types — a number often arrives quoted.
    private static int GetInt(JsonElement r, string key, int fallback) =>
        (int)Math.Round(GetFloat(r, key, fallback));

    private static float GetFloat(JsonElement r, string key, float fallback)
    {
        if (!r.TryGetProperty(key, out var v)) return fallback;
        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d)) return (float)d;
        if (v.ValueKind == JsonValueKind.String && float.TryParse(v.GetString(), out var parsed)) return parsed;
        return fallback;
    }

    private static string GetString(JsonElement r, string key) =>
        r.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static List<string> GetStringList(JsonElement r, string key) =>
        r.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Array
            ? v.EnumerateArray()
               .Where(x => x.ValueKind == JsonValueKind.String)
               .Select(x => x.GetString() ?? "")
               .Where(x => x.Length > 0)
               .ToList()
            : [];

    /// <summary>
    /// The streaming form of the chat call: same OpenAI-compatible endpoint and the same
    /// per-request deadline, read frame by frame instead of all at once.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var baseUrl = await ResolveAsync("local.baseUrl", "LocalAI:BaseUrl") ?? DefaultBaseUrl;
        var model = await ResolveAsync("local.model", "LocalAI:Model") ?? DefaultModel;
        var apiKey = await ResolveAsync("local.apiKey", "LocalAI:ApiKey");

        var body = new
        {
            model,
            max_tokens = MaxTokens,
            stream = true,
            messages = new object[] { new { role = "user", content = prompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildChatCompletionsUrl(baseUrl))
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(await TimeoutAsync());

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();

        await foreach (var payload in StreamingHttp.ReadServerSentEventsAsync(response, cts.Token))
        {
            var text = StreamingHttp.OpenAiDelta(payload);
            if (text.Length > 0) yield return text;
        }
    }

}
