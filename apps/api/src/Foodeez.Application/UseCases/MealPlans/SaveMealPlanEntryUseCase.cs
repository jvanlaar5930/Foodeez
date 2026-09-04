using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

/// <summary>Why a slot edit did or did not go through.</summary>
public enum SaveEntryOutcome
{
    Saved,
    PlanNotFound,
    EntryNotFound,
    Rejected
}

public sealed record SaveEntryResult(
    SaveEntryOutcome Outcome,
    MealPlanEntryDto? Entry = null,
    string? Reason = null);

/// <summary>
/// Adding, changing and clearing a single slot on the weekly calendar. Until this existed
/// a plan could only arrive whole from the AI - there was no way to put one meal anywhere.
/// </summary>
public class SaveMealPlanEntryUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveMealPlanEntryUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveEntryResult> AddAsync(
        Guid planId, Guid userId, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        if (await LoadOwnedPlanAsync(planId, userId) is not { } plan)
        {
            return new SaveEntryResult(SaveEntryOutcome.PlanNotFound);
        }

        var (refusal, recipe, foodItem) = await CheckAsync(plan, request);
        if (refusal is not null)
        {
            return refusal;
        }

        // The calendar shows one meal per cell, so a second entry in the same slot would be
        // invisible and unreachable. Adding into an occupied slot replaces what is there.
        ClearSlot(plan, request.EntryDate, request.MealType, exceptId: null);

        var entry = new MealPlanEntry { MealPlanId = plan.Id };
        Apply(entry, request, recipe, foodItem);
        await _unitOfWork.MealPlans.AddEntryAsync(entry);

        await _unitOfWork.SaveChangesAsync(ct);
        return new SaveEntryResult(SaveEntryOutcome.Saved, GetMealPlanUseCase.MapEntry(entry));
    }

    public async Task<SaveEntryResult> UpdateAsync(
        Guid planId, Guid entryId, Guid userId, MealPlanEntryRequest request, CancellationToken ct = default)
    {
        if (await LoadOwnedPlanAsync(planId, userId) is not { } plan)
        {
            return new SaveEntryResult(SaveEntryOutcome.PlanNotFound);
        }

        var entry = plan.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
        {
            return new SaveEntryResult(SaveEntryOutcome.EntryNotFound);
        }

        var (refusal, recipe, foodItem) = await CheckAsync(plan, request);
        if (refusal is not null)
        {
            return refusal;
        }

        // An edit can move a meal to a different day or slot, which has to displace whatever
        // was already sitting there - but never the entry being edited itself.
        ClearSlot(plan, request.EntryDate, request.MealType, exceptId: entry.Id);

        // The entry was loaded tracked, so the mutation alone is the update.
        Apply(entry, request, recipe, foodItem);

        await _unitOfWork.SaveChangesAsync(ct);
        return new SaveEntryResult(SaveEntryOutcome.Saved, GetMealPlanUseCase.MapEntry(entry));
    }

    public async Task<SaveEntryResult> DeleteAsync(
        Guid planId, Guid entryId, Guid userId, CancellationToken ct = default)
    {
        if (await LoadOwnedPlanAsync(planId, userId) is not { } plan)
        {
            return new SaveEntryResult(SaveEntryOutcome.PlanNotFound);
        }

        var entry = plan.Entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null)
        {
            // Already gone is the outcome the caller wanted, but say so plainly rather than
            // reporting a delete that did not happen.
            return new SaveEntryResult(SaveEntryOutcome.EntryNotFound);
        }

        plan.Entries.Remove(entry);
        _unitOfWork.MealPlans.RemoveEntry(entry);
        await _unitOfWork.SaveChangesAsync(ct);
        return new SaveEntryResult(SaveEntryOutcome.Saved);
    }

    /// <summary>
    /// The plan, if it is this person's. Someone else's reads as missing rather than
    /// forbidden: confirming it exists would tell an unauthorised caller something they
    /// should not learn.
    /// </summary>
    private async Task<MealPlan?> LoadOwnedPlanAsync(Guid planId, Guid userId)
    {
        var plan = await _unitOfWork.MealPlans.GetWithEntriesAsync(planId);

        return plan is not null && plan.UserId == userId ? plan : null;
    }

    /// <summary>
    /// Everything that has to hold before a slot can be written, and the rows the request
    /// pointed at. Adding and editing check exactly the same things, and used to check them
    /// in two copies of the same twenty lines.
    /// </summary>
    /// <returns>
    /// A refusal and nothing else, or no refusal and whatever the request referred to. A
    /// request naming neither a recipe nor a food item is legitimate - a slot can hold a
    /// typed-in note - so a null recipe is only a refusal when one was actually asked for.
    /// </returns>
    private async Task<(SaveEntryResult? Refusal, Recipe? Recipe, FoodItem? FoodItem)> CheckAsync(
        MealPlan plan, MealPlanEntryRequest request)
    {
        if (Validate(plan, request) is { } reason)
        {
            return (new SaveEntryResult(SaveEntryOutcome.Rejected, Reason: reason), null, null);
        }

        var recipe = await ResolveRecipeAsync(request.RecipeId);
        if (request.RecipeId.HasValue && recipe is null)
        {
            return (new SaveEntryResult(
                SaveEntryOutcome.Rejected, Reason: "That recipe no longer exists."), null, null);
        }

        var foodItem = await ResolveFoodItemAsync(request.FoodItemId);
        if (request.FoodItemId.HasValue && foodItem is null)
        {
            return (new SaveEntryResult(
                SaveEntryOutcome.Rejected, Reason: "That food item no longer exists."), null, null);
        }

        return (null, recipe, foodItem);
    }

    /// <summary>The reason the request cannot be saved, or null when it can.</summary>
    private static string? Validate(MealPlan plan, MealPlanEntryRequest request)
    {
        if (!plan.Covers(request.EntryDate))
        {
            // An entry outside the plan's range would be saved and then never shown, since
            // the calendar only ever reads days the plan covers.
            return $"That date is outside this plan, which runs {plan.StartDate:MMM d} to {plan.EndDate:MMM d}.";
        }

        if (request.RecipeId is null
            && request.FoodItemId is null
            && string.IsNullOrWhiteSpace(request.Notes))
        {
            return "Pick a recipe or give the meal a name.";
        }

        if (request.Servings <= 0)
        {
            return "Servings must be greater than zero.";
        }

        return null;
    }

    private async Task<Recipe?> ResolveRecipeAsync(Guid? recipeId) =>
        recipeId.HasValue ? await _unitOfWork.Recipes.GetByIdAsync(recipeId.Value) : null;

    private async Task<FoodItem?> ResolveFoodItemAsync(Guid? foodItemId) =>
        foodItemId.HasValue ? await _unitOfWork.FoodItems.GetByIdAsync(foodItemId.Value) : null;

    private static void Apply(
        MealPlanEntry entry, MealPlanEntryRequest request, Recipe? recipe, FoodItem? foodItem)
    {
        entry.EntryDate = request.EntryDate;
        entry.MealType = request.MealType;
        entry.RecipeId = request.RecipeId;
        entry.FoodItemId = request.FoodItemId;
        entry.Servings = request.Servings;

        var notes = request.Notes?.Trim();
        entry.Notes = string.IsNullOrEmpty(notes) ? null : notes;

        // Attaching the loaded rows means the response carries the meal's name straight
        // away, instead of the client having to refetch the plan to find out what it saved.
        entry.Recipe = recipe;
        entry.FoodItem = foodItem;
    }

    /// <summary>Removes whatever the plan says is already in that slot.</summary>
    private void ClearSlot(MealPlan plan, DateOnly date, Domain.Enums.MealType mealType, Guid? exceptId)
    {
        foreach (var occupant in plan.OccupantsOf(date, mealType, exceptId))
        {
            plan.Entries.Remove(occupant);
            _unitOfWork.MealPlans.RemoveEntry(occupant);
        }
    }
}
