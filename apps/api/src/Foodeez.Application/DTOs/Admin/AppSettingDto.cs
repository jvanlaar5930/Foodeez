using System.ComponentModel.DataAnnotations;

namespace Foodeez.Application.DTOs.Admin;

public class AppSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSecret { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpsertSettingsRequest
{
    [Required]
    public List<UpsertSettingItem> Settings { get; set; } = [];
}

public class UpsertSettingItem
{
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;
}
