namespace Foodeez.Application.DTOs.AI;

/// <summary>
/// A meal the model read out of a sentence or a photograph, split into its separate foods.
///
/// Each item's <see cref="ParsedFoodDto.ServingSize"/> is the amount that was eaten and its
/// nutrition is for that amount - one parsed line describes one helping, not a per-100g label.
/// </summary>
public class ParsedMealDto
{
    public List<ParsedFoodDto> Items { get; set; } = new();

    /// <summary>What the model had to assume, when it had to assume anything. Shown to the user.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// What a provider returns when it could not read the meal at all. No items, because a
    /// placeholder food would be logged and counted as though it had really been eaten.
    /// </summary>
    public static ParsedMealDto Unreadable => new()
    {
        Note = "That could not be read automatically. Try describing it differently, or add the items by hand."
    };
}
