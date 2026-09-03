using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IMealLogRepository : IBaseRepository<MealLog>
{
    Task<MealLog?> GetDetailedByIdAsync(Guid id);
    Task<IReadOnlyList<MealLog>> GetByUserAndDateAsync(Guid userId, DateOnly date);
    Task<IReadOnlyList<MealLog>> GetByUserAndDateRangeAsync(Guid userId, DateOnly start, DateOnly end);

    /// <summary>
    /// The user's most recently logged meals, newest first, however long ago they were - a
    /// date window would empty the list for anyone coming back after a break, which is
    /// exactly when having last week's breakfast to hand is worth most.
    /// </summary>
    Task<IReadOnlyList<MealLog>> GetRecentAsync(Guid userId, int limit);
}
