using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;

namespace Foodeez.Application.Interfaces.Services;

/// <summary>
/// Every call takes a CancellationToken because these are the slowest things the API does -
/// a self-hosted model can spend minutes on one prompt. Without a token the work carried on
/// to completion after the caller had gone, holding a connection and a GPU for an answer
/// nobody was waiting for, and nothing short of restarting the server could stop it.
/// </summary>
public interface IAIService
{
    Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default);
    Task<GeneratedMealPlanDto> GenerateMealPlanAsync(GenerateMealPlanRequest request, UserProfileDto profile, CancellationToken ct = default);
    Task<ParsedFoodDto> ParseFoodImageAsync(byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default);
    Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default);

    /// <summary>
    /// Judge a whole day of eating and say what would round it out. A result with an empty
    /// status is how a provider reports that it could not produce one.
    /// </summary>
    Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default);

    /// <summary>Estimate per-serving nutrition for a home-cooked dish from its description.</summary>
    Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default);
}
