using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IMealPlanRepository : IBaseRepository<MealPlan>
{
    Task<IReadOnlyList<MealPlan>> GetByUserIdAsync(Guid userId);
    Task<MealPlan?> GetActiveByUserIdAsync(Guid userId, DateOnly today);

    /// <summary>
    /// One plan with its entries loaded. Editing a single slot has to see the plan's other
    /// entries - to know which one the edit displaces - and the base GetByIdAsync does not
    /// load them.
    /// </summary>
    Task<MealPlan?> GetWithEntriesAsync(Guid planId);

    /// <summary>
    /// Tracks a new entry as an insert. Adding it to the plan's collection is not enough:
    /// ids are assigned in the constructor, so the change tracker sees a set key and takes
    /// the entry for an existing row, issuing an UPDATE that matches nothing.
    /// </summary>
    Task AddEntryAsync(MealPlanEntry entry);

    void RemoveEntry(MealPlanEntry entry);
}
