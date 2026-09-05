using Foodeez.Application.DTOs.Admin;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IAppLogRepository
{
    Task<(IReadOnlyList<AppLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? level = null,
        string? search = null,
        CancellationToken ct = default);

    /// <summary>The filtered slice the raw log viewer asks for, plus the total that matched it.</summary>
    Task<(IReadOnlyList<AppLog> Items, int TotalCount)> QueryAsync(AppLogQuery query, CancellationToken ct = default);

    Task<AppLog?> GetByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Deletes everything older than the cutoff, returning how many rows went.</summary>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
