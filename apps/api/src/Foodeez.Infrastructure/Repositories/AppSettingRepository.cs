using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly AppDbContext _context;

    public AppSettingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AppSetting>> GetAllAsync()
    {
        return await _context.AppSettings.OrderBy(x => x.Category).ThenBy(x => x.Key).ToListAsync();
    }

    public async Task<AppSetting?> GetByKeyAsync(string key)
    {
        return await _context.AppSettings.FindAsync(key);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        return await _context.AppSettings
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();
    }

    public async Task UpsertAsync(string key, string value, string category = "", string? description = null, bool isSecret = false)
    {
        var existing = await _context.AppSettings.FindAsync(key);
        if (existing != null)
        {
            existing.Value = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            await _context.AppSettings.AddAsync(new AppSetting
            {
                Key = key,
                Value = value,
                Category = category,
                Description = description,
                IsSecret = isSecret,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpsertManyAsync(IEnumerable<(string Key, string Value)> pairs)
    {
        var keys = pairs.Select(p => p.Key).ToList();
        var existing = await _context.AppSettings
            .Where(x => keys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key);

        foreach (var (key, value) in pairs)
        {
            if (existing.TryGetValue(key, out var setting))
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                await _context.AppSettings.AddAsync(new AppSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
