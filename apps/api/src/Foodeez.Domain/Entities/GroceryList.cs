using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

/// <summary>
/// The shopping needed for one stretch of the meal plan.
///
/// Stored rather than computed per view for two reasons: working out what a week of meals
/// costs in ingredients is an AI call, and - more importantly - the list is edited. Ticking
/// something off, swapping an item or adding what the plan never knew about are all changes
/// that have to survive closing the tab.
/// </summary>
public class GroceryList : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// The planned meals this list was compiled from. While it matches, the stored list is
    /// served as-is; when the plan changes underneath it, the list is offered as out of date
    /// rather than silently rebuilt, because rebuilding would throw away the reader's edits.
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<GroceryListItem> Items { get; set; } = new List<GroceryListItem>();

    public bool Matches(string fingerprint) => Fingerprint == fingerprint;
}
