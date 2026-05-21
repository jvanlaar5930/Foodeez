using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

public class LogMealRequest
{
    [Required]
    public Guid UserId { get; set; }

    public DateOnly LogDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public MealType MealType { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<MealLogItemRequest> Items { get; set; } = new();
}
