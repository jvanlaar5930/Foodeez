using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Application.UseCases.Grocery;
using Foodeez.Application.UseCases.MealPlans;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// A week is generated one day at a time, and the reason is entirely about failure: the
/// previous single-call version lost every day when any part of the answer went wrong, and
/// could not say which part. These tests are about what survives.
///
/// The three rules worth holding onto: a day that fails costs only that day, a run does not
/// grind on through an outage, and nothing already on the calendar is ever overwritten.
/// </summary>
public class GenerateAIMealPlanUseCaseTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMealPlanRepository> _plans = new();
    private readonly Mock<IRecipeRepository> _recipes = new();
    private readonly Mock<IMealLogRepository> _mealLogs = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IStreamingAIService> _ai = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GenerateAIMealPlanUseCaseTests()
    {
        _unitOfWork.Setup(u => u.MealPlans).Returns(_plans.Object);
        _unitOfWork.Setup(u => u.Recipes).Returns(_recipes.Object);
        _unitOfWork.Setup(u => u.MealLogs).Returns(_mealLogs.Object);
        _unitOfWork.Setup(u => u.Users).Returns(_users.Object);

        var user = User.Create("test@test.com", "hash", "Test", "User");
        user.Profile = new UserProfile { UserId = user.Id, DailyCalorieTarget = 2100, ProfileCompleted = true };
        _users.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);

        _recipes.Setup(r => r.GetOwnedByNamesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync([]);
        _plans.Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync([]);
        _mealLogs.Setup(r => r.GetByUserAndDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync([]);
    }

    private GenerateAIMealPlanUseCase Build() =>
        new(_unitOfWork.Object,
            _ai.Object,
            new PlannedMealReader(_unitOfWork.Object),
            NullLogger<GenerateAIMealPlanUseCase>.Instance);

    private GenerateMealPlanRequest Request(int days) => new()
    {
        UserId = _userId,
        StartDate = Monday,
        EndDate = Monday.AddDays(days - 1)
    };

    /// <summary>A day's worth of JSON, as the model would stream it after its narration.</summary>
    private static string Day(string recipeName) =>
        $$"""Planning this one. {"meals":[{"mealType":1,"recipeName":"{{recipeName}}","servings":1}]}""";

    /// <summary>Answers each successive call with the next entry; nulls stand for a failure.</summary>
    private void AnswersInOrder(params string?[] answers)
    {
        var call = 0;

        _ai.Setup(a => a.StreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                var answer = call < answers.Length ? answers[call] : null;
                call++;
                return One(answer);
            });

        static async IAsyncEnumerable<string> One(string? answer)
        {
            await Task.Yield();

            // A provider that swallowed its own transport error hands back nothing at all,
            // which is exactly how an outage reaches this use case.
            if (answer != null)
            {
                yield return answer;
            }
        }
    }

    private async Task<(MealPlanGenerationResultDto? Result, List<MealPlanProgressDto> Progress, string? Error)>
        RunAsync(GenerateMealPlanRequest request)
    {
        MealPlanGenerationResultDto? result = null;
        string? error = null;
        var progress = new List<MealPlanProgressDto>();

        await foreach (var streamEvent in Build().ExecuteStreamAsync(request))
        {
            switch (streamEvent.Type)
            {
                case "result":
                    result = streamEvent.Data as MealPlanGenerationResultDto;
                    break;
                case "progress" when streamEvent.Data is MealPlanProgressDto p:
                    progress.Add(p);
                    break;
                case "error":
                    error = streamEvent.Message;
                    break;
            }
        }

        return (result, progress, error);
    }

    // ── A failed day costs only that day ─────────────────────────────────────

    [Fact]
    public async Task AFailedDay_DoesNotCostTheDaysAroundIt()
    {
        AnswersInOrder(Day("Porridge"), null, Day("Eggs"));

        var (result, _, error) = await RunAsync(Request(3));

        error.Should().BeNull();
        result!.Plan!.EntriesByDate.Should().ContainKeys("2026-01-05", "2026-01-07");
        result.FailedDates.Should().ContainSingle().Which.Should().Be("2026-01-06");
        result.StoppedEarly.Should().BeFalse();
    }

    [Fact]
    public async Task TheFailedDate_IsNamedInTheSummary_SoTheReaderKnowsWhereItStopped()
    {
        AnswersInOrder(Day("Porridge"), null, Day("Eggs"));

        var (result, _, _) = await RunAsync(Request(3));

        result!.Message.Should().Contain("2026-01-06");
    }

    [Fact]
    public async Task EveryDay_ReportsItsProgress_WithItsPlaceInTheRun()
    {
        AnswersInOrder(Day("Porridge"), Day("Soup"), Day("Eggs"));

        var (_, progress, _) = await RunAsync(Request(3));

        progress.Where(p => p.Status == "planning").Should().HaveCount(3);
        progress.Where(p => p.Status == "saved").Should().HaveCount(3);
        progress.Should().AllSatisfy(p => p.TotalDays.Should().Be(3));
        progress.Select(p => p.DayNumber).Should().Contain([1, 2, 3]);
    }

    // ── An outage is not ground through ──────────────────────────────────────

    [Fact]
    public async Task TwoFailuresInARow_StopTheRun_AndKeepWhatCameBefore()
    {
        AnswersInOrder(Day("Porridge"), null, null, Day("Never asked for"));

        var (result, progress, _) = await RunAsync(Request(7));

        result!.StoppedEarly.Should().BeTrue();
        result.Plan!.EntriesByDate.Should().ContainKey("2026-01-05");
        result.FailedDates.Should().HaveCount(2);

        progress.Where(p => p.Status == "planning").Should().HaveCount(3,
            "the fourth day is never attempted once the provider looks to be down");
    }

    [Fact]
    public async Task WhenNotOneDaySucceeds_NothingIsSaved_AndTheRunIsAnError()
    {
        AnswersInOrder(null, null);

        var (result, _, error) = await RunAsync(Request(7));

        result.Should().BeNull();
        error.Should().NotBeNull();
        _plans.Verify(r => r.AddAsync(It.IsAny<MealPlan>()), Times.Never,
            "an empty plan row would be left behind for the user to clean up");
    }

    // ── What is already there stays ──────────────────────────────────────────

    [Fact]
    public async Task ASlotAlreadyPlanned_IsNeverOverwritten()
    {
        // Monday breakfast is already spoken for, in a plan of the user's own.
        var existing = new MealPlan
        {
            UserId = _userId,
            StartDate = Monday,
            EndDate = Monday,
            Name = "Mine"
        };
        existing.Entries.Add(new MealPlanEntry
        {
            MealPlanId = existing.Id,
            EntryDate = Monday,
            MealType = MealType.Breakfast,
            Notes = "Leftover pancakes",
            Servings = 1f
        });
        _plans.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync([existing]);

        // The model plans a breakfast anyway, as models do.
        AnswersInOrder(Day("Oatmeal"));

        var (result, progress, _) = await RunAsync(Request(1));

        _plans.Verify(r => r.AddEntryAsync(It.IsAny<MealPlanEntry>()), Times.Never);
        result!.KeptDates.Should().ContainSingle().Which.Should().Be("2026-01-05");
        progress.Should().Contain(p => p.Status == "kept");
    }

    [Fact]
    public async Task TheOccupiedSlot_IsNamedInThePromptAsFixed()
    {
        var existing = new MealPlan { UserId = _userId, StartDate = Monday, EndDate = Monday, Name = "Mine" };
        existing.Entries.Add(new MealPlanEntry
        {
            MealPlanId = existing.Id,
            EntryDate = Monday,
            MealType = MealType.Breakfast,
            Notes = "Leftover pancakes",
            Servings = 1f
        });
        _plans.Setup(r => r.GetByUserIdAsync(_userId)).ReturnsAsync([existing]);

        AnswersInOrder(Day("Oatmeal"));
        await RunAsync(Request(1));

        _ai.Verify(
            a => a.StreamAsync(It.Is<string>(p => p.Contains("Leftover pancakes")), It.IsAny<CancellationToken>()),
            Times.Once,
            "the model plans around what is there rather than being corrected afterwards");
    }

    // ── Each day knows what the earlier ones did ─────────────────────────────

    [Fact]
    public async Task LaterDays_AreToldWhatTheEarlierOnesPlanned()
    {
        AnswersInOrder(Day("Porridge"), Day("Soup"));

        await RunAsync(Request(2));

        _ai.Verify(
            a => a.StreamAsync(It.Is<string>(p => p.Contains("Porridge")), It.IsAny<CancellationToken>()),
            Times.Once,
            "planning every day in ignorance of the others repeats the same two dinners all week");
    }

    [Fact]
    public async Task EachDay_IsAskedForSeparately()
    {
        AnswersInOrder(Day("A"), Day("B"), Day("C"), Day("D"));

        await RunAsync(Request(4));

        _ai.Verify(a => a.StreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task AnEmptyRange_IsRefused_RatherThanProducingAnEmptyPlan()
    {
        var request = Request(1);
        request.EndDate = request.StartDate.AddDays(-1);

        var (result, _, error) = await RunAsync(request);

        result.Should().BeNull();
        error.Should().Contain("no days");
    }
}
