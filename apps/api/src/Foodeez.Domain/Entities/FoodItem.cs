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

    public NutritionalInfo NutritionalInfo { get; set; } = NutritionalInfo.Empty;
}
