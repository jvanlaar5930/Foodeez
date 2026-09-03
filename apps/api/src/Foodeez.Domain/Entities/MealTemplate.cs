using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

/// <summary>
/// A meal someone eats often, saved under a name so it can be logged again in one tap.
///
/// This is the reliable half of quick add: the items are the exact ones the user approved the
/// first time, so logging "my usual turkey sandwich" involves no search, no model, and no
/// guessing - only the same rows again.
/// </summary>
public class MealTemplate : BaseEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>The meal this is usually eaten as, pre-selected when it is logged. Null when it varies.</summary>
    public MealType? MealType { get; set; }

    /// <summary>
    /// How often it has been logged, and when last. Ordering by these puts today's likely
    /// breakfast at the front of the list rather than whatever was saved most recently.
    /// </summary>
    public int TimesUsed { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MealTemplateItem> Items { get; set; } = new List<MealTemplateItem>();

    /// <summary>The whole meal's nutrition, so a saved meal can show its calories before being logged.</summary>
    public NutritionalInfo TotalNutrition =>
        Items.Aggregate(NutritionalInfo.Empty, (acc, item) => acc + item.NutritionalInfo);
}
