using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.AI;

public class AnalyzeMealUseCase
{
    private readonly IAIService _aiService;
    private readonly IUnitOfWork _unitOfWork;

    public AnalyzeMealUseCase(IAIService aiService, IUnitOfWork unitOfWork)
    {
        _aiService = aiService;
        _unitOfWork = unitOfWork;
    }

    public async Task<MealAnalysisDto> ExecuteAsync(MealAnalysisRequest request, CancellationToken ct = default)
    {
        await MealExclusions.ApplyAsync(request, _unitOfWork);
        return await _aiService.AnalyzeMealAsync(request, ct);
    }
}
