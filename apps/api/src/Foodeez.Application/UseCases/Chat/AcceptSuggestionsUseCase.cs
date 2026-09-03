using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.UseCases.MealPlans;

namespace Foodeez.Application.UseCases.Chat;

/// <summary>Why accepting a reply's suggestions did or did not put them on the calendar.</summary>
public enum AcceptOutcome
{
    Added,
    NotFound,
    NothingToAdd
}

public sealed record AcceptResult(AcceptOutcome Outcome, List<MealPlanDto> Plans);

/// <summary>
/// Takes the meals a reply offered and puts them on the calendar.
///
/// Separate from the reply itself because a suggestion is an offer: the model proposing a
/// week of dinners should not silently rewrite someone's plan, and the moment they say yes
/// is worth recording on the message so the thread stops offering it again.
/// </summary>
public class AcceptSuggestionsUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AddPlannedMealsUseCase _addPlannedMeals;

    public AcceptSuggestionsUseCase(IUnitOfWork unitOfWork, AddPlannedMealsUseCase addPlannedMeals)
    {
        _unitOfWork = unitOfWork;
        _addPlannedMeals = addPlannedMeals;
    }

    public async Task<AcceptResult> ExecuteAsync(
        Guid userId, Guid messageId, IReadOnlyList<int> indexes, CancellationToken ct = default)
    {
        var message = await _unitOfWork.Chat.GetMessageAsync(messageId);
        if (message == null || message.Conversation.UserId != userId)
        {
            return new AcceptResult(AcceptOutcome.NotFound, new List<MealPlanDto>());
        }

        var chosen = Select(message.Suggestions.Select(ChatMapper.ToDto).ToList(), indexes);
        if (chosen.Count == 0)
        {
            return new AcceptResult(AcceptOutcome.NothingToAdd, new List<MealPlanDto>());
        }

        var plans = await _addPlannedMeals.ExecuteAsync(userId, chosen, ct);

        message.SuggestionsAcceptedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return new AcceptResult(AcceptOutcome.Added, plans);
    }

    /// <summary>
    /// The meals to file. No indexes means all of them, which is what the single "Add to
    /// plan" button sends; out-of-range indexes are ignored rather than failing the request,
    /// since a stale client asking for a meal that is no longer there wanted the rest.
    /// </summary>
    private static List<PlannedMealDto> Select(List<PlannedMealDto> suggestions, IReadOnlyList<int> indexes)
    {
        if (indexes.Count == 0)
        {
            return suggestions;
        }

        return indexes
            .Where(i => i >= 0 && i < suggestions.Count)
            .Distinct()
            .Select(i => suggestions[i])
            .ToList();
    }
}
