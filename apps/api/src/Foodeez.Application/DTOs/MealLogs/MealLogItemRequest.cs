using System.ComponentModel.DataAnnotations;

namespace Foodeez.Application.DTOs.MealLogs;

public class MealLogItemRequest
{
    [Required]
    public Guid FoodItemId { get; set; }

    [Range(0.01, 10000)]
    public float Quantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;
}
