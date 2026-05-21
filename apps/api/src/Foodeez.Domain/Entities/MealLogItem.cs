using Foodeez.Domain.Common;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class MealLogItem : BaseEntity
{
    public Guid MealLogId { get; set; }
    public Guid FoodItemId { get; set; }
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Nutritional info already scaled to the logged quantity/serving ratio.
    /// </summary>
    public NutritionalInfo NutritionalInfo { get; set; } = NutritionalInfo.Empty;

    public MealLog MealLog { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
