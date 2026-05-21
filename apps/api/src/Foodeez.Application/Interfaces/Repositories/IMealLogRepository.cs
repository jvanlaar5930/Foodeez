using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IMealLogRepository : IBaseRepository<MealLog>
{
    Task<IReadOnlyList<MealLog>> GetByUserAndDateAsync(Guid userId, DateOnly date);
    Task<IReadOnlyList<MealLog>> GetByUserAndDateRangeAsync(Guid userId, DateOnly start, DateOnly end);
}
