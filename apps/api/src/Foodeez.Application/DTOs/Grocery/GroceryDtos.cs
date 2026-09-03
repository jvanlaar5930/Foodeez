using System.ComponentModel.DataAnnotations;

namespace Foodeez.Application.DTOs.Grocery;

public class GroceryListDto
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// True when the meal plan has changed since this list was compiled. The list is still
    /// shown - it is being shopped from - but the reader is offered a rebuild.
    /// </summary>
    public bool IsStale { get; set; }

    /// <summary>How many planned meals went into it, so an empty list explains itself.</summary>
    public int PlannedMealCount { get; set; }

    public List<GroceryItemDto> Items { get; set; } = new();
}

/// <summary>
/// What the grocery tab needs to render a range in one call: the stored list if there is
/// one, and how many meals are planned - which is the difference between "nothing planned
/// yet" and "planned, not yet compiled", two states that look identical without it.
/// </summary>
public class GroceryListStateDto
{
    public GroceryListDto? List { get; set; }
    public int PlannedMealCount { get; set; }
}

public class GroceryItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public string? Source { get; set; }
    public bool IsChecked { get; set; }
    public bool IsCustom { get; set; }
    public int SortOrder { get; set; }
}

public class GenerateGroceryListRequest
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Rebuild even if the stored list still matches the plan. Costs an AI call and drops
    /// the plan-derived items, so it is never the default.
    /// </summary>
    public bool Refresh { get; set; }
}

/// <summary>Ticking one line off, without resending the rest of it.</summary>
public class SetGroceryItemCheckedRequest
{
    public bool IsChecked { get; set; }
}

/// <summary>An item added or changed by hand. The same shape serves both.</summary>
public class GroceryItemRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Quantity { get; set; } = string.Empty;

    [MaxLength(60)]
    public string Category { get; set; } = "Other";

    public bool IsChecked { get; set; }
}
