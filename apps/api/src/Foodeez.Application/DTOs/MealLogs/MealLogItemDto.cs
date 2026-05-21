namespace Foodeez.Application.DTOs.MealLogs;

public class MealLogItemDto
{
    public Guid Id { get; set; }
    public FoodItemDto FoodItem { get; set; } = null!;
    public float Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public NutritionalInfoDto NutritionalInfo { get; set; } = new();
}
