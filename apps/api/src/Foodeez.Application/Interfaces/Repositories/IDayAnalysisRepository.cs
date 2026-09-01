using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IDayAnalysisRepository : IBaseRepository<DayAnalysis>
{
    Task<DayAnalysis?> GetByUserAndDateAsync(Guid userId, DateOnly date);
}
