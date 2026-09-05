namespace Foodeez.Application.DTOs.MealLogs;

/// <summary>
/// A food somebody is adding themselves, because it is not in the database and not in USDA's
/// either - a local bakery's loaf, a family recipe measured once.
/// </summary>
public class CreateFoodItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public float ServingSize { get; set; }
    public string ServingUnit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}
