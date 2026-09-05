using System.Runtime.CompilerServices;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Reads ai.provider from AppSettings at runtime and delegates to the matching provider.
/// Registered as the active <see cref="IAIService"/> and <see cref="IStreamingAIService"/>;
/// the concrete providers are registered directly so this can resolve one by name.
/// </summary>
public sealed class DynamicAIService : IAIService, IStreamingAIService
{
    private const string DefaultProvider = "claude";

    private readonly IServiceProvider _services;
    private readonly IAppSettingRepository _settings;

    /// <summary>
    /// Resolved once per scope - that is, once per request.
    ///
    /// Every delegating member below used to call ResolveAsync, and each call was a database
    /// read of the same settings row. Generating a meal plan touches several of them, so a
    /// single request was paying for several identical SELECTs to answer a question whose
    /// answer cannot change midway through it.
    /// </summary>
    private AIProviderBase? _resolved;

    public DynamicAIService(IServiceProvider services, IAppSettingRepository settings)
    {
        _services = services;
        _settings = settings;
    }

    private async Task<AIProviderBase> ResolveAsync()
    {
        if (_resolved != null)
        {
            return _resolved;
        }

        var provider = await _settings.GetValueAsync("ai.provider") ?? DefaultProvider;

        return _resolved = provider.ToLowerInvariant() switch
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

    public async Task<ParsedMealDto> ParseMealImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
        => await (await ResolveAsync()).ParseMealImageAsync(imageData, mimeType, ct);

    public async Task<ParsedMealDto> ParseMealDescriptionAsync(string description, CancellationToken ct = default)
        => await (await ResolveAsync()).ParseMealDescriptionAsync(description, ct);

    public async Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).AnalyzeMealAsync(request, ct);

    public async Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).AnalyzeDayAsync(request, ct);

    public async Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default)
        => await (await ResolveAsync()).EstimateNutritionAsync(request, ct);

    /// <summary>
    /// Every provider streams, because <see cref="AIProviderBase"/> requires it - so this only
    /// has to pick one. It used to resolve an IAIService and check at runtime whether it also
    /// streamed, throwing if not, while the DI registration simultaneously assumed the cast
    /// always succeeded. Resolving the base type states the requirement once, in the type.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = await ResolveAsync();

        await foreach (var chunk in provider.StreamAsync(prompt, ct))
        {
            yield return chunk;
        }
    }
}
