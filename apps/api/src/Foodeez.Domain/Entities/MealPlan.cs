using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;

namespace Foodeez.Domain.Entities;

public class MealPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsAIGenerated { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MealPlanEntry> Entries { get; set; } = new List<MealPlanEntry>();

    /// <summary>Whether this plan's calendar has a cell for that day at all.</summary>
    public bool Covers(DateOnly date) => date >= StartDate && date <= EndDate;

    /// <summary>
    /// The entries already sitting in one slot, which putting something there has to displace.
    ///
    /// A slot holds one meal, because the calendar shows one cell per day and meal type - a
    /// second entry in the same cell would be saved, invisible and unreachable. That is a fact
    /// about the plan rather than about saving one, which is why it lives here; the caller
    /// decides what to do with what it hands back.
    /// </summary>
    /// <param name="exceptId">
    /// An entry that does not count as an occupant - the one being moved into the slot, which
    /// must not displace itself.
    /// </param>
    public IReadOnlyList<MealPlanEntry> OccupantsOf(DateOnly date, MealType mealType, Guid? exceptId = null) =>
        Entries
            .Where(entry => entry.EntryDate == date && entry.MealType == mealType && entry.Id != exceptId)
            .ToList();
}
