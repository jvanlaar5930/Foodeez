using System.Runtime.CompilerServices;
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

    // ── The deadline ──────────────────────────────────────────────────────────

    /// <summary>
    /// How long one call to this provider may take. Infinite unless a provider says otherwise,
    /// which every one of them now does from its own admin-editable setting.
    /// </summary>
    protected virtual ValueTask<TimeSpan> RequestTimeoutAsync() => new(Timeout.InfiniteTimeSpan);

    /// <summary>
    /// Runs one call to this provider under its deadline, translating an expiry into a
    /// failure rather than letting it escape as a cancellation.
    ///
    /// That translation is the whole point. Cancelling because the reader closed the tab and
    /// cancelling because the model is still thinking after five minutes arrive as the very
    /// same exception type, and they call for opposite responses: the first must propagate
    /// untouched, the second is a provider fault like any other and belongs on the fallback
    /// path. Only the code holding the token source can tell which fired, so it decides here
    /// and hands the rest of the class two clearly different things.
    /// </summary>
    private protected async Task<T> UnderDeadlineAsync<T>(
        Func<CancellationToken, Task<T>> work, CancellationToken ct)
    {
        var timeout = await RequestTimeoutAsync();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            cts.CancelAfter(timeout);
        }

        try
        {
            return await work(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new AIGenerationFailedException(TimedOutMessage(timeout));
        }
    }

    /// <summary>
    /// Says which provider ran out of time and how long it was given, because the fix is
    /// almost always to give it longer and the person reading this has no other way to learn
    /// what the current limit even is.
    /// </summary>
    private string TimedOutMessage(TimeSpan timeout) =>
        $"{ProviderName} did not answer within {timeout.TotalSeconds:0}s. If the model is " +
        "simply slow, raise its request timeout in Admin > Settings.";

    /// <inheritdoc />
    /// <remarks>
    /// Sealed so the deadline cannot be forgotten by a provider added later; the HTTP call
    /// itself goes in <see cref="StreamCoreAsync"/>.
    /// </remarks>
    public async IAsyncEnumerable<string> StreamAsync(
        string prompt, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var timeout = await RequestTimeoutAsync();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (timeout != Timeout.InfiniteTimeSpan)
        {
            cts.CancelAfter(timeout);
        }

        // Stepped by hand rather than with await foreach, because the deadline has to be
        // caught around each MoveNextAsync: a stream that stops halfway is exactly the case
        // this exists for, and a bare cancellation escaping here would be read by the SSE
        // writer as the reader having walked away - logged as a shrug, with the person still
        // watching told nothing at all.
        await using var chunks = StreamCoreAsync(prompt, cts.Token).GetAsyncEnumerator(cts.Token);

        while (true)
        {
            bool moved;
            try
            {
                moved = await chunks.MoveNextAsync();
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new AIGenerationFailedException(TimedOutMessage(timeout));
            }

            if (!moved)
            {
                yield break;
            }

            yield return chunks.Current;
        }
    }

    /// <summary>The provider's own streaming call, already bound to the deadline.</summary>
    protected abstract IAsyncEnumerable<string> StreamCoreAsync(string prompt, CancellationToken ct);

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
            //
            // This provider's own deadline never reaches here as a cancellation: UnderDeadline
            // turns that into an AIGenerationFailedException first, precisely so that the two
            // stay distinguishable. Deciding it here instead - by asking whether the caller's
            // token had fired - looked equivalent and was not: a request with no cancellation
            // token at all, which is most of them, made every genuine cancellation read as a
            // timeout and become a fallback.
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
        RunAsync(
            () => UnderDeadlineAsync(async token => parse(await SendAsync(prompt, token)), ct),
            fallback,
            activity,
            ct);

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
            () => UnderDeadlineAsync<ParsedMealDto?>(
                async token => await ReadMealImageAsync(imageData, mimeType, token), ct),
            () => ParsedMealDto.Unreadable,
            "read a meal from a photo",
            ct);
    }
}
