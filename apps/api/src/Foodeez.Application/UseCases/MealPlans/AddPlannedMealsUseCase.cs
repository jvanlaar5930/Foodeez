using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

/// <summary>
/// Puts a set of meals onto someone's calendar, whatever plans they happen to have.
///
/// The advice tab can propose a meal for any date, but the calendar only shows meals that
/// fall inside a plan - so a date nobody has a plan for would silently vanish. This finds the
/// plan covering each date and creates one plan spanning whatever is left over, which is why
/// it is a use case rather than a loop over the entry endpoint on the client.
/// </summary>
public class AddPlannedMealsUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public AddPlannedMealsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Files every meal, returning the plans that ended up holding them. Meals already in a
    /// slot displace what was there, matching the calendar's one-meal-per-cell rule.
    /// </summary>
    public async Task<List<MealPlanDto>> ExecuteAsync(
        Guid userId, IReadOnlyList<PlannedMealDto> meals, CancellationToken ct = default)
    {
        if (meals.Count == 0)
        {
            return new List<MealPlanDto>();
        }

        var plans = (await _unitOfWork.MealPlans.GetByUserIdAsync(userId)).ToList();

        var uncovered = meals
            .Select(m => m.Date)
            .Distinct()
            .Where(date => FindPlanFor(plans, date) == null)
            .ToList();

        if (uncovered.Count > 0)
        {
            // One plan for everything unplanned rather than one per date: a plan is a period,
            // and a week of advice should read as a week, not as seven one-day plans.
            var start = uncovered.Min();
            var end = uncovered.Max();

            var created = new MealPlan
            {
                UserId = userId,
                Name = $"Planned from advice {start:MMM d}",
                StartDate = start,
                EndDate = end,
                IsAIGenerated = true
            };

            await _unitOfWork.MealPlans.AddAsync(created);
            plans.Add(created);
        }

        var touched = new HashSet<Guid>();

        foreach (var meal in meals)
        {
            var plan = FindPlanFor(plans, meal.Date);
            if (plan == null)
            {
                continue;
            }

            ClearSlot(plan, meal);

            var entry = new MealPlanEntry
            {
                MealPlanId = plan.Id,
                EntryDate = meal.Date,
                MealType = meal.MealType,
                Notes = Label(meal),
                Servings = meal.Servings > 0 ? meal.Servings : 1f
            };

            plan.Entries.Add(entry);
            await _unitOfWork.MealPlans.AddEntryAsync(entry);
            touched.Add(plan.Id);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return plans
            .Where(p => touched.Contains(p.Id))
            .Select(GetMealPlanUseCase.MapToDto)
            .ToList();
    }

    /// <summary>
    /// The plan covering a date. When several do, the one that starts latest wins - it is
    /// the most recently planned week, and so the one the user is looking at.
    /// </summary>
    private static MealPlan? FindPlanFor(IEnumerable<MealPlan> plans, DateOnly date) =>
        plans
            .Where(p => p.StartDate <= date && p.EndDate >= date)
            .OrderByDescending(p => p.StartDate)
            .FirstOrDefault();

    private void ClearSlot(MealPlan plan, PlannedMealDto meal)
    {
        var occupants = plan.Entries
            .Where(e => e.EntryDate == meal.Date && e.MealType == meal.MealType)
            .ToList();

        foreach (var occupant in occupants)
        {
            plan.Entries.Remove(occupant);
            _unitOfWork.MealPlans.RemoveEntry(occupant);
        }
    }

    /// <summary>
    /// The name is all a reader sees: these entries have no recipe row behind them, so Notes
    /// is the one field that reaches the calendar card.
    /// </summary>
    private static string Label(PlannedMealDto meal) =>
        string.IsNullOrWhiteSpace(meal.Description)
            ? meal.Name
            : $"{meal.Name} - {meal.Description}";
}
