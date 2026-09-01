using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class DayAnalysisRepository : BaseRepository<DayAnalysis>, IDayAnalysisRepository
{
    public DayAnalysisRepository(AppDbContext context) : base(context) { }

    public async Task<DayAnalysis?> GetByUserAndDateAsync(Guid userId, DateOnly date)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.UserId == userId && d.LogDate == date);
    }
}
