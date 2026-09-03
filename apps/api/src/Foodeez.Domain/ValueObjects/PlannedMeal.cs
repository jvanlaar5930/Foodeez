using Foodeez.Domain.Enums;

namespace Foodeez.Domain.ValueObjects;

/// <summary>
/// A meal the assistant proposed during a conversation, in the shape a calendar slot needs.
///
/// Kept on the message that suggested it rather than written straight to the plan: advice is
/// a conversation, and nothing should land on someone's calendar until they say so. Once they
/// do, this is everything required to fill the slot.
/// </summary>
public class PlannedMeal
{
    public DateOnly Date { get; set; }
    public MealType MealType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public float Servings { get; set; } = 1f;
}
