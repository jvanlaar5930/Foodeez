using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Everything the AI providers have in common, which turned out to be almost all of it.
///
/// Each provider used to implement all six features itself, and the implementations were the
/// same shape every time: build a prompt from a class in Application/Common, send it, parse
/// the answer with the matching parser, and on failure log and return a fallback - wrapped in
/// an identical try/catch pair, thirty-odd copies of it across the five files. What actually
/// differs between providers is the HTTP call and the shape of the response, so that is all a
/// subclass now supplies.
///
/// The cancellation case is the reason the envelope is worth having in one place rather than
/// pasted: a cancelled request must rethrow, not be logged as a provider fault and flattened
/// into an empty result that the caller would go on to treat as real data. That distinction is
/// easy to get subtly wrong, and did not need re-deciding five times.
/// </summary>
public abstract class AIProviderBase : IAIService, IStreamingAIService
{
    protected AIProviderBase(ILogger logger) => Logger = logger;

    protected ILogger Logger { get; }

    /// <summary>Used in log messages, so an entry says which provider actually failed.</summary>
    protected abstract string ProviderName { get; }

    /// <summary>One prompt in, the model's whole answer out.</summary>
    protected abstract Task<string> SendAsync(string prompt, CancellationToken ct);

    /// <inheritdoc />
    public abstract IAsyncEnumerable<string> StreamAsync(string prompt, CancellationToken ct = default);

    // ── The shared envelope ───────────────────────────────────────────────────

    /// <summary>
    /// Runs one call to the provider, turning any failure into <paramref name="fallback"/>.
    ///
    /// This is the envelope every feature shares. Cancellation is the reason it is worth
    /// having in one place: a cancelled request must rethrow rather than be logged as a
    /// provider fault and flattened into an empty result that the caller would treat as data.
    /// </summary>
    /// <param name="attempt">Returns null when the provider answered with nothing usable.</param>
    /// <param name="fallback">Built lazily, so it costs nothing on the happy path.</param>
    /// <param name="activity">
    /// Named in the log line, phrased to complete "Groq: failed to ..." - e.g. "analyze the day".
    /// </param>
    protected async Task<T> RunAsync<T>(
        Func<Task<T?>> attempt,
        Func<T> fallback,
        string activity,
        CancellationToken ct)
        where T : class
    {
        try
        {
            var result = await attempt();
            if (result != null)
            {
                return result;
            }

            Logger.LogWarning("{Provider}: returned nothing usable when asked to {Activity}.", ProviderName, activity);
        }
        catch (OperationCanceledException)
        {
            // The caller gave up. That is not a provider failure and must not be logged as
            // one, nor flattened into an empty result the caller would treat as data.
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Provider}: failed to {Activity}.", ProviderName, activity);
        }

        return fallback();
    }

    /// <summary>The common case: send one prompt, parse the answer, fall back if either fails.</summary>
    protected Task<T> ExecuteAsync<T>(
        string prompt,
        Func<string, T?> parse,
        Func<T> fallback,
        string activity,
        CancellationToken ct)
        where T : class =>
        RunAsync(async () => parse(await SendAsync(prompt, ct)), fallback, activity, ct);

    // ── IAIService, once ──────────────────────────────────────────────────────

    public Task<DietaryRecommendationsDto> GetDietaryRecommendationsAsync(
        UserProfileDto profile, DailyNutritionDto? recentNutrition = null, CancellationToken ct = default) =>
        ExecuteAsync(
            RecommendationsPrompt.Build(profile, recentNutrition),
            RecommendationsPrompt.Parse,
            () => RecommendationsPrompt.Fallback(profile),
            "produce dietary recommendations",
            ct);

    public Task<ParsedMealDto> ParseMealDescriptionAsync(string description, CancellationToken ct = default) =>
        ExecuteAsync(
            MealParsePrompt.BuildText(description),
            MealParsePrompt.Parse,
            () => ParsedMealDto.Unreadable,
            "read a described meal",
            ct);

    public Task<MealAnalysisDto> AnalyzeMealAsync(MealAnalysisRequest request, CancellationToken ct = default) =>
        ExecuteAsync(
            MealAnalysisPrompt.Build(request),
            MealAnalysisPrompt.Parse,
            () => new MealAnalysisDto
            {
                Score = 0,
                Completeness = "Analysis unavailable.",
                Missing = [],
                Suggestions = []
            },
            "analyze a meal",
            ct);

    public Task<DayAnalysisDto> AnalyzeDayAsync(DayAnalysisRequest request, CancellationToken ct = default) =>
        ExecuteAsync(
            DayAnalysisPrompt.Build(request),
            DayAnalysisPrompt.Parse,
            // An empty analysis is how a provider outage reaches the caller; nothing is stored for it.
            () => new DayAnalysisDto(),
            "analyze the day",
            ct);

    public Task<EstimatedNutritionDto> EstimateNutritionAsync(EstimateNutritionRequest request, CancellationToken ct = default) =>
        ExecuteAsync(
            NutritionEstimation.BuildPrompt(request),
            NutritionEstimation.Parse,
            () => new EstimatedNutritionDto
            {
                Succeeded = false,
                Confidence = "low",
                Assumptions = "We could not estimate this one automatically. Enter the values you know."
            },
            "estimate nutrition",
            ct);

    // ── Vision, which not every provider has ──────────────────────────────────

    /// <summary>
    /// Whether this provider can read a photograph. A provider that cannot returns no items
    /// and says why, rather than a placeholder food: a made-up "Unknown Food" would be logged
    /// and counted as though someone had really eaten it.
    ///
    /// Asynchronous because for a self-hosted server the answer is not a fact about the
    /// provider but a runtime setting - whether a vision model happens to be loaded - which
    /// lives in the database.
    /// </summary>
    protected virtual Task<bool> SupportsVisionAsync() => Task.FromResult(false);

    /// <summary>Why this provider cannot see, phrased for the person who just took the photo.</summary>
    protected virtual string VisionUnsupportedNote =>
        $"Photos need a provider that can see, and {ProviderName} cannot. Describe the meal instead.";

    /// <summary>Implemented only by providers whose <see cref="SupportsVisionAsync"/> can be true.</summary>
    protected virtual Task<ParsedMealDto> ReadMealImageAsync(byte[] imageData, string? mimeType, CancellationToken ct) =>
        throw new NotSupportedException($"{ProviderName} does not support vision.");

    public async Task<ParsedMealDto> ParseMealImageAsync(
        byte[] imageData, string? mimeType = "image/jpeg", CancellationToken ct = default)
    {
        if (!await SupportsVisionAsync())
        {
            Logger.LogWarning("{Provider}: asked to read a meal from a photo, which it cannot do.", ProviderName);
            return new ParsedMealDto { Note = VisionUnsupportedNote };
        }

        // RunAsync rather than ExecuteAsync: an image request is not one prompt string, so the
        // subclass builds and sends the whole thing itself and only the envelope is shared.
        return await RunAsync(
            async () => await ReadMealImageAsync(imageData, mimeType, ct),
            () => ParsedMealDto.Unreadable,
            "read a meal from a photo",
            ct);
    }
}
