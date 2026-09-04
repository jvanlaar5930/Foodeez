using FluentAssertions;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Domain.Tests.Entities;

/// <summary>
/// A slot holds one meal, because the calendar shows one cell per day and meal type - a
/// second entry in the same cell is saved, invisible and unreachable. That rule used to live
/// inside the use case that saves a slot.
/// </summary>
public class MealPlanTests
{
    private static MealPlan Week() => new()
    {
        StartDate = new DateOnly(2026, 3, 2),
        EndDate = new DateOnly(2026, 3, 8)
    };

    private static MealPlanEntry Entry(MealPlan plan, DateOnly date, MealType mealType)
    {
        var entry = new MealPlanEntry { MealPlanId = plan.Id, EntryDate = date, MealType = mealType };
        plan.Entries.Add(entry);
        return entry;
    }

    [Fact]
    public void Covers_TheFirstAndLastDay()
    {
        var plan = Week();

        plan.Covers(new DateOnly(2026, 3, 2)).Should().BeTrue();
        plan.Covers(new DateOnly(2026, 3, 8)).Should().BeTrue();
    }

    [Fact]
    public void DoesNotCover_TheDaysEitherSide()
    {
        var plan = Week();

        plan.Covers(new DateOnly(2026, 3, 1)).Should().BeFalse();
        plan.Covers(new DateOnly(2026, 3, 9)).Should().BeFalse();
    }

    [Fact]
    public void AnEmptySlot_HasNoOccupants()
    {
        Week().OccupantsOf(new DateOnly(2026, 3, 3), MealType.Lunch).Should().BeEmpty();
    }

    [Fact]
    public void TheEntryInThatSlot_IsTheOccupant()
    {
        var plan = Week();
        var lunch = Entry(plan, new DateOnly(2026, 3, 3), MealType.Lunch);

        plan.OccupantsOf(new DateOnly(2026, 3, 3), MealType.Lunch).Should().Equal(lunch);
    }

    [Fact]
    public void EntriesInOtherSlots_AreNotOccupants()
    {
        var plan = Week();
        Entry(plan, new DateOnly(2026, 3, 3), MealType.Breakfast);
        Entry(plan, new DateOnly(2026, 3, 4), MealType.Lunch);

        plan.OccupantsOf(new DateOnly(2026, 3, 3), MealType.Lunch).Should().BeEmpty();
    }

    [Fact]
    public void AnEntryDoesNotDisplaceItself()
    {
        // Editing a meal without moving it must not delete the meal being edited.
        var plan = Week();
        var lunch = Entry(plan, new DateOnly(2026, 3, 3), MealType.Lunch);

        plan.OccupantsOf(new DateOnly(2026, 3, 3), MealType.Lunch, exceptId: lunch.Id)
            .Should().BeEmpty();
    }

    [Fact]
    public void MovingOntoAnOccupiedSlot_ReportsWhatIsThere()
    {
        var plan = Week();
        var sitting = Entry(plan, new DateOnly(2026, 3, 5), MealType.Dinner);
        var moving = Entry(plan, new DateOnly(2026, 3, 3), MealType.Dinner);

        plan.OccupantsOf(new DateOnly(2026, 3, 5), MealType.Dinner, exceptId: moving.Id)
            .Should().Equal(sitting);
    }
}
