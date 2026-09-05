using FluentAssertions;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Enums;
using Foodeez.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Foodeez.Infrastructure.Tests.Services;

/// <summary>
/// AIProviderBase now owns the behaviour that used to be pasted into all five providers -
/// about thirty copies of the same try/catch. That makes it the single place where getting
/// the failure handling wrong would break every AI feature at once, so it is worth testing on
/// its own rather than only through a provider.
///
/// The cancellation case is the one that actually matters: a cancelled request must rethrow.
/// If it were swallowed like any other exception, the caller would receive the fallback - an
/// empty analysis, a blank recommendation - and go on to store and display it as though the
/// model had really said that.
/// </summary>
public class AIProviderBaseTests
{
    /// <summary>A provider whose "HTTP call" is whatever the test hands it.</summary>
    private sealed class FakeProvider : AIProviderBase
    {
        private readonly Func<string, CancellationToken, Task<string>> _send;

        public FakeProvider(
            Func<string, CancellationToken, Task<string>> send,
            ILogger? logger = null,
            bool supportsVision = false,
            Func<Task<ParsedMealDto>>? readImage = null)
            : base(logger ?? NullLogger.Instance)
        {
            _send = send;
            Vision = supportsVision;
            ReadImage = readImage;
        }

        private bool Vision { get; }
        private Func<Task<ParsedMealDto>>? ReadImage { get; }

        public string? LastPrompt { get; private set; }
        public int SendCount { get; private set; }

        protected override string ProviderName => "Fake";

        protected override Task<bool> SupportsVisionAsync() => Task.FromResult(Vision);

        protected override Task<string> SendAsync(string prompt, CancellationToken ct)
        {
            LastPrompt = prompt;
            SendCount++;
            return _send(prompt, ct);
        }

        protected override Task<ParsedMealDto> ReadMealImageAsync(
            byte[] imageData, string? mimeType, CancellationToken ct) =>
            ReadImage?.Invoke() ?? throw new NotSupportedException();

        public override async IAsyncEnumerable<string> StreamAsync(
            string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            await Task.Yield();
            yield return await _send(prompt, ct);
        }
    }

    private static FakeProvider Answering(string response, ILogger? logger = null) =>
        new((_, _) => Task.FromResult(response), logger);

    private static FakeProvider Throwing(Exception ex, ILogger? logger = null) =>
        new((_, _) => Task.FromException<string>(ex), logger);

    private static UserProfileDto Profile() => new()
    {
        UserId = Guid.NewGuid(),
        Age = 30,
        Gender = Gender.Male,
        WeightKg = 80,
        HeightCm = 180,
        DailyCalorieTarget = 2259,
        DailyProteinTargetG = 64,
        DietaryGoal = DietaryGoal.WeightLoss,
        ActivityLevel = ActivityLevel.ModeratelyActive
    };

    private static MealAnalysisRequest MealRequest() => new() { MealType = "Breakfast" };

    // ── The happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task AnalyzeMeal_UsableAnswer_IsReturned()
    {
        var provider = Answering("""{"score":81,"completeness":"Balanced.","missing":[],"suggestions":["More fibre"]}""");

        var result = await provider.AnalyzeMealAsync(MealRequest());

        result.Score.Should().Be(81);
        result.Completeness.Should().Be("Balanced.");
    }

    [Fact]
    public async Task EveryFeature_SendsAPromptThatIsNotEmpty()
    {
        // Each feature is wired to a prompt builder in Application/Common; this is what would
        // catch one of those wirings being dropped in the collapse.
        var provider = Answering("{}");

        await provider.GetDietaryRecommendationsAsync(Profile());
        provider.LastPrompt.Should().NotBeNullOrWhiteSpace();

        await provider.AnalyzeMealAsync(MealRequest());
        provider.LastPrompt.Should().NotBeNullOrWhiteSpace();

        await provider.AnalyzeDayAsync(new DayAnalysisRequest { Date = new DateOnly(2026, 1, 5) });
        provider.LastPrompt.Should().NotBeNullOrWhiteSpace();

        await provider.EstimateNutritionAsync(new EstimateNutritionRequest { Name = "stew" });
        provider.LastPrompt.Should().NotBeNullOrWhiteSpace();

        await provider.ParseMealDescriptionAsync("two eggs on toast");
        provider.LastPrompt.Should().NotBeNullOrWhiteSpace();

        provider.SendCount.Should().Be(5);
    }

    // ── Cancellation must not be swallowed ────────────────────────────────────

    [Fact]
    public async Task Cancellation_IsRethrown_NotTurnedIntoAFallback()
    {
        var provider = Throwing(new OperationCanceledException());

        var act = () => provider.AnalyzeDayAsync(new DayAnalysisRequest { Date = new DateOnly(2026, 1, 5) });

        await act.Should().ThrowAsync<OperationCanceledException>(
            because: "a fallback would be stored and shown as though the model had said it");
    }

    [Fact]
    public async Task Cancellation_IsNotLoggedAsAProviderFailure()
    {
        var logger = new Mock<ILogger>();
        var provider = Throwing(new OperationCanceledException(), logger.Object);

        try
        {
            await provider.AnalyzeMealAsync(MealRequest());
        }
        catch (OperationCanceledException)
        {
            // expected
        }

        logger.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                       It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task CancellationSubclasses_AreAlsoRethrown()
    {
        // A timeout arrives as TaskCanceledException, which derives from OperationCanceledException.
        var provider = Throwing(new TaskCanceledException());

        var act = () => provider.EstimateNutritionAsync(new EstimateNutritionRequest { Name = "stew" });

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    // ── Failures become fallbacks ─────────────────────────────────────────────

    [Fact]
    public async Task TransportFailure_BecomesTheFallback_AndIsLogged()
    {
        var logger = new Mock<ILogger>();
        var provider = Throwing(new HttpRequestException("503"), logger.Object);

        var result = await provider.AnalyzeMealAsync(MealRequest());

        result.Score.Should().Be(0);
        result.Completeness.Should().Be("Analysis unavailable.");

        logger.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                       It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UnparseableAnswer_BecomesTheFallback_AndIsWarnedAbout()
    {
        var logger = new Mock<ILogger>();
        var provider = Answering("I'm afraid I can't do that.", logger.Object);

        var result = await provider.AnalyzeDayAsync(new DayAnalysisRequest { Date = new DateOnly(2026, 1, 5) });

        // An empty analysis is how an outage reaches the caller; nothing is stored for it.
        result.Should().NotBeNull();

        logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                       It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecommendationsFallback_IsBuiltFromTheProfile_NotEmpty()
    {
        var provider = Throwing(new HttpRequestException("down"));

        var result = await provider.GetDietaryRecommendationsAsync(Profile());

        result.OverallScore.Should().Be(50);
        result.Suggestions.Should().NotBeEmpty(because: "an outage should not show a blank page");
        result.Suggestions.Should().Contain(s => s.Contains("2259"));
    }

    [Fact]
    public async Task NutritionEstimateFallback_SaysItDidNotSucceed()
    {
        var provider = Throwing(new HttpRequestException("down"));

        var result = await provider.EstimateNutritionAsync(new EstimateNutritionRequest { Name = "stew" });

        result.Succeeded.Should().BeFalse();
        result.Confidence.Should().Be("low");
    }

    // ── Vision ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WithoutVision_ReturnsNoItemsAndSaysWhy()
    {
        // Not a placeholder food: a made-up "Unknown Food" would be logged and counted as
        // though someone had really eaten it.
        var provider = Answering("{}");

        var result = await provider.ParseMealImageAsync([1, 2, 3]);

        result.Items.Should().BeEmpty();
        result.Note.Should().NotBeNullOrWhiteSpace();
        provider.SendCount.Should().Be(0, because: "there is no point sending a photo to a model that cannot see");
    }

    [Fact]
    public async Task WithVision_DelegatesToTheProvider()
    {
        var expected = new ParsedMealDto { Note = "read it" };
        var provider = new FakeProvider(
            (_, _) => Task.FromResult("{}"), supportsVision: true, readImage: () => Task.FromResult(expected));

        (await provider.ParseMealImageAsync([1, 2, 3])).Should().BeSameAs(expected);
    }

    [Fact]
    public async Task WithVision_AFailedReadStillFallsBack()
    {
        var provider = new FakeProvider(
            (_, _) => Task.FromResult("{}"),
            supportsVision: true,
            readImage: () => throw new HttpRequestException("boom"));

        var result = await provider.ParseMealImageAsync([1, 2, 3]);

        // Unreadable is a fresh instance each time, so compare what it says rather than identity.
        result.Items.Should().BeEmpty();
        result.Note.Should().Be(ParsedMealDto.Unreadable.Note);
    }

    [Fact]
    public async Task WithVision_ACancelledReadIsStillRethrown()
    {
        var provider = new FakeProvider(
            (_, _) => Task.FromResult("{}"),
            supportsVision: true,
            readImage: () => throw new OperationCanceledException());

        var act = () => provider.ParseMealImageAsync([1, 2, 3]);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
