using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// Analyses a user's whole day of eating: where it stands against their targets and what to
/// eat next to round it out. Like the per-meal analysis, the result is stored and reused -
/// a day is only re-analysed once its meals or targets change, or on an explicit refresh.
/// </summary>
public class AnalyzeDayUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;
    private readonly IStreamingAIService _streaming;
    private readonly GetNutritionSummaryUseCase _getNutritionSummary;

    public AnalyzeDayUseCase(
        IUnitOfWork unitOfWork,
        IAIService aiService,
        IStreamingAIService streaming,
        GetNutritionSummaryUseCase getNutritionSummary)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _streaming = streaming;
        _getNutritionSummary = getNutritionSummary;
    }

    /// <summary>
    /// The stored analysis for the day, or null when there is none or the day has moved on
    /// since it was written. Never calls the model, so a page load costs nothing.
    /// </summary>
    public async Task<DayAnalysisDto?> PeekAsync(Guid userId, DateOnly date)
    {
        var stored = await _unitOfWork.DayAnalyses.GetByUserAndDateAsync(userId, date);
        if (stored == null)
        {
            return null;
        }

        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        var summary = await _getNutritionSummary.ExecuteAsync(userId, date);
        var profile = (await _unitOfWork.Users.GetByIdAsync(userId))?.Profile;

        return stored.Matches(MealAnalysisFingerprint.ForDay(date, logs, summary, Exclusions(profile)))
            ? ToDto(stored)
            : null;
    }

    public async Task<DayAnalysisDto> ExecuteAsync(
        Guid userId,
        DateOnly date,
        bool refresh = false,
        CancellationToken ct = default)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        if (logs.Count == 0)
        {
            throw new InvalidOperationException("There are no meals logged on this day to analyze.");
        }

        var summary = await _getNutritionSummary.ExecuteAsync(userId, date);
        var profile = (await _unitOfWork.Users.GetByIdAsync(userId))?.Profile;
        var fingerprint = MealAnalysisFingerprint.ForDay(date, logs, summary, Exclusions(profile));
        var stored = await _unitOfWork.DayAnalyses.GetByUserAndDateAsync(userId, date);

        if (!refresh && stored != null && stored.Matches(fingerprint))
        {
            return ToDto(stored);
        }

        var result = await _aiService.AnalyzeDayAsync(BuildRequest(date, profile, summary, logs), ct);

        // Providers swallow their own transport errors and hand back an empty analysis. Storing
        // that would leave the user with a permanently blank score for the day, so refuse it.
        if (string.IsNullOrWhiteSpace(result.Status) && result.Recommendations.Count == 0)
        {
            throw new AIGenerationFailedException(
                "The AI service could not analyze your day right now. Please try again in a moment.");
        }

        return await StoreAsync(userId, date, stored, result, fingerprint, ct);
    }

    private static void Apply(DayAnalysis entity, DayAnalysisDto result, string fingerprint, DateTime generatedAt)
    {
        entity.Score = result.Score;
        entity.Status = result.Status;
        entity.Gaps = result.Gaps.ToList();
        entity.Recommendations = result.Recommendations.ToList();
        entity.Fingerprint = fingerprint;
        entity.GeneratedAt = generatedAt;
    }

    private static DayAnalysisDto ToDto(DayAnalysis entity) => new()
    {
        Score = entity.Score,
        Status = entity.Status,
        Gaps = entity.Gaps.ToList(),
        Recommendations = entity.Recommendations.ToList(),
        GeneratedAt = entity.GeneratedAt
    };

    private static DayAnalysisMealRequest ToMealRequest(MealLog log)
    {
        var total = log.TotalNutrition;

        return new DayAnalysisMealRequest
        {
            MealType = log.MealType.ToString(),
            Items = log.Items
                .Select(i => $"{i.Quantity:0.##}{i.Unit} {i.FoodItem.Name}")
                .ToList(),
            Calories = total.Calories,
            Protein = total.Protein,
            Carbs = total.Carbohydrates,
            Fat = total.Fat,
            Fiber = total.Fiber
        };
    }

    /// <summary>
    /// The same analysis, streamed as the model writes it. A stored analysis arrives as a
    /// single result with nothing to watch - it was already paid for.
    /// </summary>
    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        Guid userId,
        DateOnly date,
        bool refresh = false,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        if (logs.Count == 0)
        {
            yield return AIStreamEvent.Error("There are no meals logged on this day to analyze.");
            yield break;
        }

        var summary = await _getNutritionSummary.ExecuteAsync(userId, date);
        var profile = (await _unitOfWork.Users.GetByIdAsync(userId))?.Profile;
        var fingerprint = MealAnalysisFingerprint.ForDay(date, logs, summary, Exclusions(profile));
        var stored = await _unitOfWork.DayAnalyses.GetByUserAndDateAsync(userId, date);

        if (!refresh && stored != null && stored.Matches(fingerprint))
        {
            yield return AIStreamEvent.Result(ToDto(stored));
            yield break;
        }

        var prompt = DayAnalysisPrompt.Build(BuildRequest(date, profile, summary, logs));

        var transcript = new StringBuilder();
        await foreach (var delta in AINarration.NarrateAsync(_streaming, prompt, transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var result = DayAnalysisPrompt.Parse(transcript.ToString());
        if (result == null)
        {
            yield return AIStreamEvent.Error(
                "The AI service could not analyze your day right now. Please try again in a moment.");
            yield break;
        }

        yield return AIStreamEvent.Result(await StoreAsync(userId, date, stored, result, fingerprint, ct));
    }

    private async Task<DayAnalysisDto> StoreAsync(
        Guid userId,
        DateOnly date,
        DayAnalysis? stored,
        DayAnalysisDto result,
        string fingerprint,
        CancellationToken ct)
    {
        var isNew = stored == null;
        stored ??= new DayAnalysis { UserId = userId, LogDate = date };

        Apply(stored, result, fingerprint, DateTime.UtcNow);

        if (isNew)
        {
            await _unitOfWork.DayAnalyses.AddAsync(stored);
        }
        else
        {
            _unitOfWork.DayAnalyses.Update(stored);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return ToDto(stored);
    }

    private static DayAnalysisRequest BuildRequest(
        DateOnly date,
        UserProfile? profile,
        NutritionSummaryDto summary,
        IEnumerable<MealLog> logs) => new()
    {
        Date = date,
        IsToday = date == DateOnly.FromDateTime(DateTime.UtcNow),
        DietaryGoal = profile?.DietaryGoal.ToString(),
        ExcludedFoods = profile?.ExcludedFoods.ToList() ?? new List<string>(),
        Summary = summary,
        Meals = logs.Select(ToMealRequest).ToList()
    };

    private static List<string> Exclusions(UserProfile? profile) =>
        profile?.ExcludedFoods.ToList() ?? new List<string>();
}
