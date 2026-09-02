using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealLogs;

/// <summary>A meal in the words its eater would use: "turkey sandwich, an apple and a coffee".</summary>
public class QuickAddRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}

/// <summary>Where a quick-added line's numbers came from. Shown per item so nothing is taken on trust.</summary>
public enum QuickAddSource
{
    /// <summary>Straight out of a saved meal the user built and approved earlier.</summary>
    Saved,

    /// <summary>Matched to a food already in the database, whose nutrition is used as-is.</summary>
    Matched,

    /// <summary>Nothing matched, so these figures are the model's estimate and want a glance.</summary>
    Estimated
}

public class QuickAddItemDto
{
    public FoodItemDto FoodItem { get; set; } = new();

    /// <summary>The amount eaten, in <see cref="Unit"/>. Ready to drop straight into a meal log item.</summary>
    public float Quantity { get; set; }

    public string Unit { get; set; } = string.Empty;

    public QuickAddSource Source { get; set; }

    /// <summary>0.0-1.0, how sure the model was about this line. 1.0 for anything it did not have to guess.</summary>
    public float Confidence { get; set; }
}

/// <summary>
/// What quick add worked out, for the user to look over before saving. Nothing here has been
/// logged: the client drops these items into the meal dialog, where they can be corrected,
/// removed or added to exactly as if they had been searched for one at a time.
/// </summary>
public class QuickAddResultDto
{
    public List<QuickAddItemDto> Items { get; set; } = new();

    /// <summary>The saved meal this came from, when a name matched one. Null when the model read it.</summary>
    public Guid? MealTemplateId { get; set; }
    public string? MealTemplateName { get; set; }

    /// <summary>The meal type to pre-select, taken from the saved meal. Null when there is nothing to go on.</summary>
    public MealType? MealType { get; set; }

    /// <summary>Anything the user should know: what was assumed, or why nothing came back.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// True when a saved meal answered this, meaning no AI call was made and the items are
    /// exactly the ones the user approved last time.
    /// </summary>
    public bool FromSavedMeal => MealTemplateId.HasValue;
}
