using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

public class MealLogDto
{
    public Guid Id { get; set; }
    public DateOnly LogDate { get; set; }
    public MealType MealType { get; set; }
    public string? Notes { get; set; }
    public List<MealLogItemDto> Items { get; set; } = new();
    public NutritionalInfoDto TotalNutrition { get; set; } = new();
}
