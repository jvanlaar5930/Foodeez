using Foodeez.Domain.Common;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Domain.Entities;

public class MealTemplateItem : BaseEntity
{
    public Guid MealTemplateId { get; set; }
    public Guid FoodItemId { get; set; }
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Nutrition already scaled to this quantity, stored the same way a logged meal's items
    /// store it. Keeping it here means a saved meal's totals do not shift under the user if
    /// the underlying food item is later re-synced from USDA with different numbers.
    /// </summary>
    public NutritionalInfo NutritionalInfo { get; set; } = NutritionalInfo.Empty;

    public MealTemplate MealTemplate { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
