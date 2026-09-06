using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.MealPlans;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

/// <summary>
/// Dragging a meal from one cell to another.
///
/// The behaviour worth pinning is what happens to the meal already in the destination. Editing
/// a slot deletes it, which is right when someone has deliberately chosen a replacement - and
/// wrong for a drag, where the destination is wherever the cursor happened to be released and
/// the meal being destroyed is one nobody pointed at. These say it swaps instead, and that both
/// ends of the swap come back so the calendar can redraw two cells rather than one.
/// </summary>
public class MoveMealPlanEntryUseCaseTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMealPlanRepository> _plans = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly MealPlan _plan;

    public MoveMealPlanEntryUseCaseTests()
    {
        _unitOfWork.Setup(u => u.MealPlans).Returns(_plans.Object);

        _plan = new MealPlan
        {
            UserId = _userId,
            StartDate = Monday,
            EndDate = Monday.AddDays(6)
        };

        _plans.Setup(r => r.GetWithEntriesAsync(It.IsAny<Guid>())).ReturnsAsync(_plan);
    }

    private SaveMealPlanEntryUseCase Build() => new(_unitOfWork.Object);

    private MealPlanEntry Entry(string name, DateOnly date, MealType mealType)
    {
        var entry = new MealPlanEntry
        {
            MealPlanId = _plan.Id,
            EntryDate = date,
            MealType = mealType,
            Notes = name,
            Servings = 1f
        };

        _plan.Entries.Add(entry);
        return entry;
    }

    private static MealPlanEntryMoveRequest To(DateOnly date, MealType mealType) =>
        new() { EntryDate = date, MealType = mealType };

    [Fact]
    public async Task MovingToAnEmptySlot_TakesTheMealThereAndLeavesNothingBehind()
    {
        var porridge = Entry("Porridge", Monday, MealType.MorningSnack);

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday.AddDays(1), MealType.Lunch));

        result.Outcome.Should().Be(SaveEntryOutcome.Saved);
        porridge.EntryDate.Should().Be(Monday.AddDays(1));
        porridge.MealType.Should().Be(MealType.Lunch);
        result.Swapped.Should().BeNull("nothing was in the destination");
        _plan.OccupantsOf(Monday, MealType.MorningSnack).Should().BeEmpty();
    }

    [Fact]
    public async Task MovingOntoAnOccupiedSlot_SwapsTheTwoMealsRatherThanDestroyingOne()
    {
        var porridge = Entry("Porridge", Monday, MealType.MorningSnack);
        var salad = Entry("Salad", Monday.AddDays(1), MealType.Lunch);

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday.AddDays(1), MealType.Lunch));

        result.Outcome.Should().Be(SaveEntryOutcome.Saved);

        porridge.EntryDate.Should().Be(Monday.AddDays(1));
        porridge.MealType.Should().Be(MealType.Lunch);

        salad.EntryDate.Should().Be(Monday, "the displaced meal takes the dragged one's old place");
        salad.MealType.Should().Be(MealType.MorningSnack);

        _plan.Entries.Should().HaveCount(2, "a swap must not delete either meal");
    }

    [Fact]
    public async Task ASwap_ReportsBothEndsSoTheCalendarCanRedrawTwoCells()
    {
        var porridge = Entry("Porridge", Monday, MealType.MorningSnack);
        var salad = Entry("Salad", Monday.AddDays(1), MealType.Lunch);

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday.AddDays(1), MealType.Lunch));

        result.Entry!.Id.Should().Be(porridge.Id);
        result.Swapped!.Id.Should().Be(salad.Id);
        result.Swapped.MealType.Should().Be(MealType.MorningSnack);
    }

    [Fact]
    public async Task MovingOutsideThePlansDates_IsRefusedAndChangesNothing()
    {
        var porridge = Entry("Porridge", Monday, MealType.Breakfast);

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday.AddDays(30), MealType.Breakfast));

        result.Outcome.Should().Be(SaveEntryOutcome.Rejected);
        result.Reason.Should().NotBeNullOrWhiteSpace();
        porridge.EntryDate.Should().Be(Monday, "a refused move must not half-apply");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MovingAMealBackOntoItself_IsSavedWithoutSwappingItWithItself()
    {
        var porridge = Entry("Porridge", Monday, MealType.Breakfast);

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday, MealType.Breakfast));

        result.Outcome.Should().Be(SaveEntryOutcome.Saved);
        result.Swapped.Should().BeNull("an entry is not an occupant of its own slot");
        porridge.EntryDate.Should().Be(Monday);
        porridge.MealType.Should().Be(MealType.Breakfast);
    }

    // ── Across plans ─────────────────────────────────────────────────────────
    //
    // The mobile day screen creates a plan covering only the day it was opened on, so picking
    // a different day there always means leaving the plan the meal is in. Without this the
    // feature would refuse every move it was built for.

    /// <summary>A second plan, covering a day the first one does not.</summary>
    private MealPlan OtherPlan(DateOnly day)
    {
        var other = new MealPlan { UserId = _userId, StartDate = day, EndDate = day };
        _plans.Setup(r => r.GetWithEntriesAsync(other.Id)).ReturnsAsync(other);
        return other;
    }

    [Fact]
    public async Task MovingIntoAnotherPlan_ReassignsTheMealToIt()
    {
        var porridge = Entry("Porridge", Monday, MealType.MorningSnack);
        var nextWeek = OtherPlan(Monday.AddDays(20));

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId,
            new MealPlanEntryMoveRequest
            {
                EntryDate = Monday.AddDays(20),
                MealType = MealType.Lunch,
                TargetPlanId = nextWeek.Id
            });

        result.Outcome.Should().Be(SaveEntryOutcome.Saved);
        porridge.MealPlanId.Should().Be(nextWeek.Id);
        porridge.EntryDate.Should().Be(Monday.AddDays(20));
        porridge.MealType.Should().Be(MealType.Lunch);
    }

    [Fact]
    public async Task ACrossPlanSwap_SendsTheDisplacedMealBackToTheOtherPlan()
    {
        var porridge = Entry("Porridge", Monday, MealType.MorningSnack);

        var nextWeek = OtherPlan(Monday.AddDays(20));
        var salad = new MealPlanEntry
        {
            MealPlanId = nextWeek.Id,
            EntryDate = Monday.AddDays(20),
            MealType = MealType.Lunch,
            Notes = "Salad",
            Servings = 1f
        };
        nextWeek.Entries.Add(salad);

        await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId,
            new MealPlanEntryMoveRequest
            {
                EntryDate = Monday.AddDays(20),
                MealType = MealType.Lunch,
                TargetPlanId = nextWeek.Id
            });

        porridge.MealPlanId.Should().Be(nextWeek.Id);
        salad.MealPlanId.Should().Be(_plan.Id, "the swap has to carry the plan, not just the slot");
        salad.EntryDate.Should().Be(Monday);
        salad.MealType.Should().Be(MealType.MorningSnack);
    }

    [Fact]
    public async Task ADateOutsideTheTargetPlan_IsStillRefused()
    {
        var porridge = Entry("Porridge", Monday, MealType.Breakfast);
        var nextWeek = OtherPlan(Monday.AddDays(20));

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId,
            new MealPlanEntryMoveRequest
            {
                EntryDate = Monday.AddDays(25),
                MealType = MealType.Lunch,
                TargetPlanId = nextWeek.Id
            });

        result.Outcome.Should().Be(SaveEntryOutcome.Rejected);
        porridge.MealPlanId.Should().Be(_plan.Id);
    }

    [Fact]
    public async Task SomeoneElsesPlan_ReadsAsMissingRatherThanForbidden()
    {
        var porridge = Entry("Porridge", Monday, MealType.Breakfast);
        _plan.UserId = Guid.NewGuid();

        var result = await Build().MoveAsync(
            _plan.Id, porridge.Id, _userId, To(Monday, MealType.Lunch));

        result.Outcome.Should().Be(SaveEntryOutcome.PlanNotFound);
    }

    [Fact]
    public async Task AnEntryThatIsNotInThisPlan_IsReportedAsMissing()
    {
        var result = await Build().MoveAsync(
            _plan.Id, Guid.NewGuid(), _userId, To(Monday, MealType.Lunch));

        result.Outcome.Should().Be(SaveEntryOutcome.EntryNotFound);
    }
}
