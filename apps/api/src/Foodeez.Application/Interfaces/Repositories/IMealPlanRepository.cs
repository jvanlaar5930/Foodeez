using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IMealPlanRepository : IBaseRepository<MealPlan>
{
    Task<IReadOnlyList<MealPlan>> GetByUserIdAsync(Guid userId);
    Task<MealPlan?> GetActiveByUserIdAsync(Guid userId, DateOnly today);
}
