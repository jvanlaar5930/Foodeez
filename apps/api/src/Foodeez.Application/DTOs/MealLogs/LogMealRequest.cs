using System.ComponentModel.DataAnnotations;
using Foodeez.Application.DTOs.AI;
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

    /// <summary>
    /// An analysis the client already generated for exactly these items, stored with the meal
    /// so the same score does not have to be paid for again when the meal is opened later.
    /// </summary>
    public MealAnalysisDto? Analysis { get; set; }
}
