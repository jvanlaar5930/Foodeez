using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.AI;

/// <summary>
/// Estimates per-serving nutrition for a home-cooked dish, so someone logging their own
/// cooking is not asked for macros nobody could reasonably know.
/// </summary>
public class EstimateNutritionUseCase
{
    private readonly IAIService _ai;

    public EstimateNutritionUseCase(IAIService ai)
    {
        _ai = ai;
    }

    public async Task<EstimatedNutritionDto> ExecuteAsync(EstimateNutritionRequest request, CancellationToken ct = default)
    {
        // Guard the batch size here rather than in the prompt: dividing by zero servings would
        // silently produce whole-batch figures presented as a single portion.
        if (request.Servings < 1) request.Servings = 1;
        if (request.Servings > 100) request.Servings = 100;

        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Ingredients = string.IsNullOrWhiteSpace(request.Ingredients)
            ? null
            : request.Ingredients.Trim();

        return await _ai.EstimateNutritionAsync(request, ct);
    }
}
