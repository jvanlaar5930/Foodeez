using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

public class MealTemplateItemDto
{
    public FoodItemDto FoodItem { get; set; } = new();
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public NutritionalInfoDto NutritionalInfo { get; set; } = new();
}

public class MealTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public MealType? MealType { get; set; }
    public List<MealTemplateItemDto> Items { get; set; } = new();
    public NutritionalInfoDto TotalNutrition { get; set; } = new();
    public int TimesUsed { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// Save the meal currently on screen under a name. The items are sent rather than a meal log
/// id so a meal can be saved while it is still being built, before it has ever been logged.
/// </summary>
public class SaveMealTemplateRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public MealType? MealType { get; set; }

    [Required]
    [MinLength(1)]
    public List<MealLogItemRequest> Items { get; set; } = new();
}

/// <summary>A meal the user logged before, offered up for logging again as it stands.</summary>
public class RecentMealDto
{
    /// <summary>The meal log this came from, so "log it again" can copy its items.</summary>
    public Guid MealLogId { get; set; }
    public DateOnly LogDate { get; set; }
    public MealType MealType { get; set; }

    /// <summary>The meal read as a line of text - "Rye bread, Turkey breast, Mayonnaise".</summary>
    public string Summary { get; set; } = string.Empty;

    public List<MealTemplateItemDto> Items { get; set; } = new();
    public NutritionalInfoDto TotalNutrition { get; set; } = new();

    /// <summary>True when this exact set of items is already saved under a name.</summary>
    public bool IsSaved { get; set; }
}
