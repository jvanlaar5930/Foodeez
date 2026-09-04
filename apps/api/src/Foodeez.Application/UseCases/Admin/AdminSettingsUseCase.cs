using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Admin;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Admin;

/// <summary>
/// The runtime settings table an administrator can read and change - which AI provider is
/// active, and the keys some of them need.
/// </summary>
public class AdminSettingsUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AdminSettingsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AppSettingDto>> ListAsync()
    {
        var settings = await _unitOfWork.AppSettings.GetAllAsync();

        return settings.Select(ToDto).ToList();
    }

    public async Task UpsertAsync(UpsertSettingsRequest request)
    {
        await _unitOfWork.AppSettings.UpsertManyAsync(
            request.Settings.Select(setting => (setting.Key, setting.Value)));
    }

    private static AppSettingDto ToDto(AppSetting setting) => new()
    {
        Key = setting.Key,
        Value = setting.IsSecret ? Mask(setting.Value) : setting.Value,
        Category = setting.Category,
        Description = setting.Description,
        IsSecret = setting.IsSecret,
        UpdatedAt = setting.UpdatedAt
    };

    /// <summary>
    /// Enough of a key to recognise which one it is, not enough to use it. A short value is
    /// hidden entirely: masking "abc12345" would leave nothing hidden.
    /// </summary>
    private static string Mask(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= 8) return "****";

        return value[..4] + new string('*', value.Length - 8) + value[^4..];
    }
}
