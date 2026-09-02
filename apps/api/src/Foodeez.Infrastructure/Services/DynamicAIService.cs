using System.Runtime.CompilerServices;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

// Reads ai.provider from AppSettings at runtime and delegates to the correct IAIService impl.
// Registered as IAIService; concrete services (Claude, Gemini, Groq, Ollama, Local) registered directly.
public class DynamicAIService : IAIService, IStreamingAIService
{
    private readonly IServiceProvider _services;
    private readonly IAppSettingRepository _settings;
    private readonly ILogger<DynamicAIService> _logger;

    public DynamicAIService(IServiceProvider services, IAppSettingRepository settings, ILogger<DynamicAIService> logger)
    {
        _services = services;
        _settings = settings;
        _logger = logger;
    }

    private async Task<IAIService> ResolveAsync()
    {
        var provider = await _settings.GetValueAsync("ai.provider") ?? "claude";

        return provider.ToLowerInvariant() switch
        {
            "gemini" => _services.GetRequiredService<GeminiAIService>(),
            "groq" => _services.GetRequiredService<GroqAIService>(),
            "ollama" => _services.GetRequiredService<OllamaAIService>(),
            // One OpenAI-compatible client covers every self-hosted server; the aliases are
            // accepted so a value someone typed by hand still lands on it.
            "local" or "lmstudio" or "lm-studio" or "llamacpp" or "llama.cpp" or "llama-cpp"
                or "localai" or "vllm" or "jan" or "openai-compatible"
                => _services.GetRequiredService<LocalAIService>(),
            _ => _services.GetRequiredService<ClaudeAIService>()
        };
    }

    public async Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default)
        => await (await ResolveAsync()).GetDietaryRecommendationsAsync(profile, recentNutrition, ct);

    public async Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default)
        => await (await ResolveAsync()).GenerateMealPlanAsync(request, profile, ct);

    public async Task<ParsedFoodDto> ParseFoodImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
        => await (await ResolveAsync()).ParseFoodImageAsync(imageData, mimeType, ct);

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).AnalyzeMealAsync(request, ct);

    public async Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).AnalyzeDayAsync(request, ct);

    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).EstimateNutritionAsync(request, ct);

    /// <summary>
    /// Every provider here streams, so this only has to pick one. A provider that did not
    /// would have to say so rather than quietly return nothing, since the caller reads the
    /// finished answer out of the streamed text.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = await ResolveAsync();
        if (provider is not IStreamingAIService streaming)
        {
            throw new NotSupportedException(
                $"The configured AI provider ({provider.GetType().Name}) cannot stream a response.");
        }

        await foreach (var chunk in streaming.StreamAsync(prompt, ct))
        {
            yield return chunk;
        }
    }

}
