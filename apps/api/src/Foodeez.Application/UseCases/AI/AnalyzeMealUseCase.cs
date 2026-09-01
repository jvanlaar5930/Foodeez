using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.AI;

public class AnalyzeMealUseCase
{
    private readonly IAIService _aiService;

    public AnalyzeMealUseCase(IAIService aiService)
    {
        _aiService = aiService;
    }

    public Task<MealAnalysisDto> ExecuteAsync(MealAnalysisRequest request, CancellationToken ct = default) =>
        _aiService.AnalyzeMealAsync(request, ct);
}
