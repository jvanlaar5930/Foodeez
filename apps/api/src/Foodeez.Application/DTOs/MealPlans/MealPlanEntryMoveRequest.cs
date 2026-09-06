using System.ComponentModel.DataAnnotations;
using Foodeez.Domain.Enums;

namespace Foodeez.Application.DTOs.MealPlans;

/// <summary>
/// Where a meal is being dragged to. Only the destination, deliberately.
///
/// A move could be expressed as a <see cref="MealPlanEntryRequest"/> with two fields changed,
/// but that would have the client resend the recipe, servings and notes it is not touching -
/// and a client that rebuilt any of that slightly wrong would silently rewrite the meal while
/// appearing only to move it.
/// </summary>
public class MealPlanEntryMoveRequest
{
    [Required]
    public DateOnly EntryDate { get; set; }

    [Required]
    public MealType MealType { get; set; }

    /// <summary>
    /// The plan the destination day belongs to, when it is not the plan the meal is in now.
    ///
    /// A destination is often in another plan: the day screen creates a plan covering only the
    /// day it was opened on, so on that calendar *any* change of date leaves the plan. Without
    /// this the move would be refused as out of range, which is true and useless - the day
    /// exists, it is simply filed under a different plan.
    ///
    /// The caller resolves it because only the caller knows whether a plan for that day should
    /// be created; a move is not the place to start making plans nobody asked for.
    /// </summary>
    public Guid? TargetPlanId { get; set; }
}

/// <summary>
/// What a move changed. Both cells are returned because a swap moves two meals, and a client
/// that only heard about one of them would leave the other drawn in its old slot until the
/// next refetch.
/// </summary>
public class MealPlanEntryMoveResponse
{
    /// <summary>The meal that was dragged, in its new place.</summary>
    public MealPlanEntryDto Entry { get; set; } = null!;

    /// <summary>The meal it swapped with, in the place the dragged one left. Null if the destination was empty.</summary>
    public MealPlanEntryDto? Swapped { get; set; }
}
