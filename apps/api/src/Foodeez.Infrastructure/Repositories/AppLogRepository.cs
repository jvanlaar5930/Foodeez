using Foodeez.Application.DTOs.Admin;
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
        CancellationToken ct = default) =>
        await QueryAsync(
            new AppLogQuery(Level: level, Search: search, Limit: pageSize, Offset: (page - 1) * pageSize), ct);

    public async Task<(IReadOnlyList<AppLog> Items, int TotalCount)> QueryAsync(
        AppLogQuery query, CancellationToken ct = default)
    {
        // Logs are read to look at, never to edit, so there is nothing for the change tracker
        // to do with several hundred of them.
        var rows = _context.AppLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Level))
            rows = rows.Where(x => x.Level == query.Level);

        if (!string.IsNullOrWhiteSpace(query.Source))
            rows = rows.Where(x => x.Source != null && x.Source.Contains(query.Source));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // No ToLower(): the MySQL collation this runs on compares case-insensitively
            // already, and calling it stops the comparison using an index.
            var search = query.Search;
            rows = rows.Where(x =>
                x.Message.Contains(search) ||
                (x.Source != null && x.Source.Contains(search)) ||
                (x.ExceptionMessage != null && x.ExceptionMessage.Contains(search)));
        }

        if (query.From is { } from)
            rows = rows.Where(x => x.Timestamp >= from.ToUniversalTime());

        if (query.To is { } to)
            rows = rows.Where(x => x.Timestamp <= to.ToUniversalTime());

        var total = await rows.CountAsync(ct);
        var items = await rows
            .OrderByDescending(x => x.Timestamp)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<AppLog?> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _context.AppLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default) =>
        await _context.AppLogs.Where(x => x.Timestamp < cutoff).ExecuteDeleteAsync(ct);
}
