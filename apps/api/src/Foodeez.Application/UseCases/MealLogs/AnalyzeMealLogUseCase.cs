using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// Returns the AI analysis of a saved meal, generating one only when the meal has none for
/// its current items - or when the caller explicitly asks for a fresh one. Anything generated
/// here is stored on the meal, so a score is paid for once rather than on every view.
/// </summary>
public class AnalyzeMealLogUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;

    public AnalyzeMealLogUseCase(IUnitOfWork unitOfWork, IAIService aiService)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
    }

    public async Task<MealAnalysisDto> ExecuteAsync(Guid mealLogId, bool refresh = false, CancellationToken ct = default)
    {
        var mealLog = await _unitOfWork.MealLogs.GetDetailedByIdAsync(mealLogId);
        if (mealLog == null)
        {
            throw new KeyNotFoundException($"MealLog with id '{mealLogId}' was not found.");
        }

        var fingerprint = MealAnalysisFingerprint.For(mealLog);

        if (!refresh && mealLog.Analysis is { } stored && stored.Matches(fingerprint))
        {
            return MealAnalysisMapper.ToDto(stored);
        }

        var result = await _aiService.AnalyzeMealAsync(MealAnalysisMapper.ToRequest(mealLog), ct);

        mealLog.Analysis = new MealAnalysis(
            result.Score,
            result.Completeness,
            result.Missing,
            result.Suggestions,
            fingerprint,
            DateTime.UtcNow);

        _unitOfWork.MealLogs.Update(mealLog);
        await _unitOfWork.SaveChangesAsync(ct);

        return MealAnalysisMapper.ToDto(mealLog.Analysis);
    }
}
