namespace Foodeez.Application.DTOs.MealLogs;

public class FoodItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public float ServingSize { get; set; }
    public string ServingUnit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public NutritionalInfoDto NutritionalInfo { get; set; } = new();
}
