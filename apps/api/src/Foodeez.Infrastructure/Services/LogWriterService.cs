using Foodeez.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Foodeez.Infrastructure.Services;

/// <summary>
/// Background service that drains the LogQueue and persists entries to the database
/// in batches, isolated from the request pipeline.
/// </summary>
public sealed class LogWriterService : BackgroundService
{
    private readonly LogQueue _queue;
    private readonly IServiceProvider _services;
    private readonly ILogger<LogWriterService> _logger;

    private const int BatchSize = 50;
    private const int FlushIntervalMs = 2000;

    public LogWriterService(LogQueue queue, IServiceProvider services, ILogger<LogWriterService> logger)
    {
        _queue   = queue;
        _services = services;
        _logger  = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait until at least one entry is available
                await _queue.Reader.WaitToReadAsync(stoppingToken);

                var batch = new List<Domain.Entities.AppLog>(BatchSize);

                // Drain up to BatchSize entries without blocking
                while (batch.Count < BatchSize && _queue.Reader.TryRead(out var entry))
                    batch.Add(entry);

                if (batch.Count > 0)
                    await PersistAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogWriterService encountered an unexpected error.");
                await Task.Delay(FlushIntervalMs, stoppingToken);
            }
        }

        // Flush remaining entries on shutdown
        var remaining = new List<Domain.Entities.AppLog>();
        while (_queue.Reader.TryRead(out var e)) remaining.Add(e);
        if (remaining.Count > 0)
            await PersistAsync(remaining, CancellationToken.None);
    }

    private async Task PersistAsync(List<Domain.Entities.AppLog> batch, CancellationToken ct)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.AppLogs.AddRangeAsync(batch, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist {Count} log entries to database.", batch.Count);
        }
    }
}
