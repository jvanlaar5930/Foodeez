using Foodeez.Domain.Common;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class FoodItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public float ServingSize { get; set; }
    public string ServingUnit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public bool IsCustom { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public int? FdcId { get; set; }
    public DateTime? FdcSyncedAt { get; set; }

    /// <summary>The figures on the label, for one <see cref="ServingSize"/> of this food.</summary>
    public NutritionalInfo NutritionalInfo { get; set; } = NutritionalInfo.Empty;

    /// <summary>
    /// What eating this much of this food comes to.
    ///
    /// The quantity is in the same unit as <see cref="ServingSize"/>, so 150 g of a food
    /// labelled per 100 g is one and a half servings. A food with no serving size recorded -
    /// which happens for hand-entered ones - is read as being labelled per unit, so the
    /// quantity is the multiplier directly. That last rule is why this belongs on the entity:
    /// it was written out at three call sites, and it is the kind of thing that gets copied
    /// as two of the three.
    /// </summary>
    public NutritionalInfo NutritionFor(float quantity) =>
        NutritionalInfo.Scale(ServingSize > 0 ? quantity / ServingSize : quantity);
}
