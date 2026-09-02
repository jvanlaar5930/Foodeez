using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
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
    private readonly IStreamingAIService _streaming;

    public AnalyzeMealLogUseCase(IUnitOfWork unitOfWork, IAIService aiService, IStreamingAIService streaming)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _streaming = streaming;
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

    /// <summary>
    /// The same analysis, streamed as the model writes it. A stored analysis still short-
    /// circuits the model entirely: it arrives as a single result with nothing to watch,
    /// which is the point of having stored it.
    /// </summary>
    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        Guid mealLogId,
        bool refresh = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var mealLog = await _unitOfWork.MealLogs.GetDetailedByIdAsync(mealLogId);
        if (mealLog == null)
        {
            throw new KeyNotFoundException($"MealLog with id '{mealLogId}' was not found.");
        }

        var fingerprint = MealAnalysisFingerprint.For(mealLog);

        if (!refresh && mealLog.Analysis is { } stored && stored.Matches(fingerprint))
        {
            yield return AIStreamEvent.Result(MealAnalysisMapper.ToDto(stored));
            yield break;
        }

        var transcript = new StringBuilder();
        var prompt = MealAnalysisPrompt.Build(MealAnalysisMapper.ToRequest(mealLog));

        await foreach (var delta in AINarration.NarrateAsync(_streaming, prompt, transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var result = MealAnalysisPrompt.Parse(transcript.ToString());
        if (result == null)
        {
            yield return AIStreamEvent.Error(
                "The AI service could not analyze this meal right now. Please try again in a moment.");
            yield break;
        }

        yield return AIStreamEvent.Result(await StoreAsync(mealLog, result, fingerprint, ct));
    }

    private async Task<MealAnalysisDto> StoreAsync(
        MealLog mealLog,
        MealAnalysisDto result,
        string fingerprint,
        CancellationToken ct)
    {
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
