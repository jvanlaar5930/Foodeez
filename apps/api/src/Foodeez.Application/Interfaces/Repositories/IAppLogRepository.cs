using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IAppLogRepository
{
    Task<(IReadOnlyList<AppLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? level = null,
        string? search = null,
        CancellationToken ct = default);
}
