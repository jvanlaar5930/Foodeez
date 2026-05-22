using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IAppSettingRepository
{
    Task<IReadOnlyList<AppSetting>> GetAllAsync();
    Task<AppSetting?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task UpsertAsync(string key, string value, string category = "", string? description = null, bool isSecret = false);
    Task UpsertManyAsync(IEnumerable<(string Key, string Value)> pairs);
}
