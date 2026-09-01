using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class AppLogRepository : IAppLogRepository
{
    private readonly AppDbContext _context;

    public AppLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<AppLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? level = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var query = _context.AppLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
            query = query.Where(x => x.Level == level);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(x =>
                x.Message.ToLower().Contains(lower) ||
                (x.Source != null && x.Source.ToLower().Contains(lower)) ||
                (x.ExceptionMessage != null && x.ExceptionMessage.ToLower().Contains(lower)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
