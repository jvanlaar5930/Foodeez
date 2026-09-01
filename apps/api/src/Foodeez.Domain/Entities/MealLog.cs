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
    /// Aggregates all items' nutritional info using the + operator.
    /// Returns NutritionalInfo.Empty if there are no items.
    /// </summary>
    public NutritionalInfo TotalNutrition
    {
        get
        {
            if (!Items.Any())
                return NutritionalInfo.Empty;

            return Items.Aggregate(NutritionalInfo.Empty, (acc, item) => acc + item.NutritionalInfo);
        }
    }
}
