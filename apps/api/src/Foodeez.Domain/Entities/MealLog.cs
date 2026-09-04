using Foodeez.Domain.Common;
using Foodeez.Domain.Enums;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class MealLog : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public MealType MealType { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// The stored AI analysis of this meal, or null while none has been generated for the
    /// meal in its current form.
    /// </summary>
    public MealAnalysis? Analysis { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MealLogItem> Items { get; set; } = new List<MealLogItem>();

    /// <summary>
    /// Everything in this meal, added up. Empty when there is nothing in it.
    ///
    /// Computed on every read, so a caller adding this up across many logs should read it
    /// once per log rather than once per nutrient.
    /// </summary>
    public NutritionalInfo TotalNutrition =>
        Items.Aggregate(NutritionalInfo.Empty, (running, item) => running + item.NutritionalInfo);
}
