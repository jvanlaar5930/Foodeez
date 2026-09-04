using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Admin;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Admin;

/// <summary>
/// Reading and pruning the application log.
///
/// Two controllers used to do this, and one of them (LogsController) held an AppDbContext and
/// wrote its own LINQ - reaching straight past the Application layer into EF, and returning
/// an anonymous type so no client could be generated from it. Both go through here now.
/// </summary>
public class AppLogsUseCase
{
    /// <summary>Enough to page through a busy hour; more than a person reads at once.</summary>
    private const int MaxLimit = 500;
    private const int MaxPageSize = 200;

    private readonly IUnitOfWork _unitOfWork;

    public AppLogsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>The summary page the admin log viewer shows.</summary>
    public async Task<AppLogPagedDto> GetPageAsync(
        int page, int pageSize, string? level, string? search, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        page = Math.Max(1, page);

        var (items, total) = await _unitOfWork.AppLogs.GetPagedAsync(page, pageSize, level, search, ct);

        return new AppLogPagedDto
        {
            Items = items.Select(ToSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>The full entries, filtered - stack traces, request context and all.</summary>
    public async Task<AppLogDetailPageDto> QueryAsync(AppLogQuery query, CancellationToken ct = default)
    {
        query = query with
        {
            Limit = Math.Clamp(query.Limit, 1, MaxLimit),
            Offset = Math.Max(0, query.Offset)
        };

        var (items, total) = await _unitOfWork.AppLogs.QueryAsync(query, ct);

        return new AppLogDetailPageDto
        {
            Total = total,
            Offset = query.Offset,
            Limit = query.Limit,
            Entries = items.Select(ToDetail).ToList()
        };
    }

    public async Task<AppLogDetailDto?> GetAsync(long id, CancellationToken ct = default) =>
        await _unitOfWork.AppLogs.GetByIdAsync(id, ct) is { } entry ? ToDetail(entry) : null;

    /// <summary>Deletes entries older than the given age, returning how many went.</summary>
    public async Task<int> PruneAsync(int olderThanDays, CancellationToken ct = default)
    {
        // A zero or negative age would delete everything up to this instant, which is not
        // something anyone means by "prune".
        olderThanDays = Math.Max(1, olderThanDays);

        return await _unitOfWork.AppLogs.DeleteOlderThanAsync(
            DateTime.UtcNow.AddDays(-olderThanDays), ct);
    }

    private static AppLogDto ToSummary(AppLog log) => new()
    {
        Id = log.Id,
        Level = log.Level,
        Message = log.Message,
        Exception = log.ExceptionMessage,
        Source = log.Source,
        Timestamp = log.Timestamp
    };

    private static AppLogDetailDto ToDetail(AppLog log) => new()
    {
        Id = log.Id,
        Timestamp = log.Timestamp,
        Level = log.Level,
        Message = log.Message,
        Source = log.Source,
        ExceptionType = log.ExceptionType,
        ExceptionMessage = log.ExceptionMessage,
        StackTrace = log.StackTrace,
        RequestMethod = log.RequestMethod,
        RequestPath = log.RequestPath,
        StatusCode = log.StatusCode,
        UserId = log.UserId,
        AdditionalData = log.AdditionalData
    };
}
