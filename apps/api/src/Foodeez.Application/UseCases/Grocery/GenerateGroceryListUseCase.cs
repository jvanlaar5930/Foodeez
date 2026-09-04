using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Grocery;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Grocery;

/// <summary>
/// Compiles the meal plan into a shopping list, and hands back the one already compiled.
///
/// The stored list is the working copy: it is ticked off, edited and added to, so a rebuild
/// is only ever asked for explicitly. When the plan changes underneath a list the list is
/// marked stale rather than replaced - throwing away a half-shopped list to stay in step
/// with the calendar would be the wrong trade.
/// </summary>
public class GenerateGroceryListUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStreamingAIService _streaming;
    private readonly PlannedMealReader _reader;

    public GenerateGroceryListUseCase(
        IUnitOfWork unitOfWork, IStreamingAIService streaming, PlannedMealReader reader)
    {
        _unitOfWork = unitOfWork;
        _streaming = streaming;
        _reader = reader;
    }

    /// <summary>
    /// The list already stored for this range, or null when there is none. Never calls the
    /// AI, so it is safe on every page load.
    /// </summary>
    public async Task<GroceryListDto?> PeekAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var stored = await _unitOfWork.GroceryLists.GetByRangeAsync(userId, startDate, endDate);
        if (stored == null)
        {
            return null;
        }

        var meals = await _reader.ReadAsync(userId, startDate, endDate);
        var fingerprint = GroceryListFingerprint.For(startDate, endDate, meals, await ExclusionsAsync(userId));

        return GroceryListMapper.ToDto(stored, meals.Count, isStale: !stored.Matches(fingerprint));
    }

    /// <summary>How many meals are planned in a range, for a client deciding whether to offer a build.</summary>
    public async Task<int> CountPlannedMealsAsync(Guid userId, DateOnly startDate, DateOnly endDate) =>
        (await _reader.ReadAsync(userId, startDate, endDate)).Count;

    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        Guid userId,
        GenerateGroceryListRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (request.EndDate < request.StartDate)
        {
            yield return AIStreamEvent.Error("Pick an end date on or after the start date.");
            yield break;
        }

        var meals = await _reader.ReadAsync(userId, request.StartDate, request.EndDate);
        if (meals.Count == 0)
        {
            yield return AIStreamEvent.Error(
                "There are no planned meals in those dates yet. Plan some meals first, then come back for the list.");
            yield break;
        }

        var exclusions = await ExclusionsAsync(userId);
        var fingerprint = GroceryListFingerprint.For(request.StartDate, request.EndDate, meals, exclusions);

        var stored = await _unitOfWork.GroceryLists.GetByRangeAsync(userId, request.StartDate, request.EndDate);
        if (stored != null && stored.Matches(fingerprint) && !request.Refresh)
        {
            // Already compiled from exactly these meals. Costs no AI call, and arrives with
            // no deltas - the client shows the finished list.
            yield return AIStreamEvent.Result(GroceryListMapper.ToDto(stored, meals.Count, isStale: false));
            yield break;
        }

        var transcript = new StringBuilder();
        var prompt = GroceryListPrompt.Build(meals, exclusions, request.StartDate, request.EndDate);

        await foreach (var delta in AINarration.NarrateAsync(_streaming, prompt, transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var items = GroceryListPrompt.Parse(transcript.ToString());
        if (items.Count == 0)
        {
            // A week of meals cannot honestly need nothing, so an empty list is an outage.
            // Saving it would replace a working list with a blank one.
            yield return AIStreamEvent.Error(
                "The AI service could not put a shopping list together right now. Please try again in a moment.");
            yield break;
        }

        var list = await PersistAsync(userId, request, fingerprint, items, stored, ct);
        yield return AIStreamEvent.Result(GroceryListMapper.ToDto(list, meals.Count, isStale: false));
    }

    /// <summary>
    /// Writes the new list over the plan-derived part of the old one. Hand-added items and
    /// the ticks against items that are still needed both survive: the first because only
    /// the shopper knows about them, the second because a rebuild half way round the shop
    /// should not un-tick the trolley.
    /// </summary>
    private async Task<GroceryList> PersistAsync(
        Guid userId,
        GenerateGroceryListRequest request,
        string fingerprint,
        List<GroceryItemDto> items,
        GroceryList? stored,
        CancellationToken ct)
    {
        var list = stored;

        if (list == null)
        {
            list = new GroceryList
            {
                UserId = userId,
                StartDate = request.StartDate,
                EndDate = request.EndDate
            };

            await _unitOfWork.GroceryLists.AddAsync(list);
        }

        var checkedNames = list.Items
            .Where(i => i.IsChecked)
            .Select(i => i.Name.Trim().ToLowerInvariant())
            .ToHashSet();

        foreach (var replaced in list.Items.Where(i => !i.IsCustom).ToList())
        {
            list.Items.Remove(replaced);
            _unitOfWork.GroceryLists.RemoveItem(replaced);
        }

        // Hand-added items keep the top of the list; the compiled ones follow in aisle order.
        var order = list.Items.Count;

        foreach (var item in items
                     .OrderBy(i => GroceryListPrompt.CategoryRank(i.Category))
                     .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            var entity = new GroceryListItem
            {
                GroceryListId = list.Id,
                Name = item.Name,
                Quantity = item.Quantity,
                Category = item.Category,
                Source = item.Source,
                IsChecked = checkedNames.Contains(item.Name.Trim().ToLowerInvariant()),
                IsCustom = false,
                SortOrder = order++
            };

            list.Items.Add(entity);
            await _unitOfWork.GroceryLists.AddItemAsync(entity);
        }

        list.Fingerprint = fingerprint;
        list.GeneratedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return list;
    }

    /// <summary>
    /// The user's standing exclusions, read from the profile rather than taken from the
    /// caller - a shopping list is a list of food to buy, and an allergy must not depend on
    /// a client remembering to send it.
    /// </summary>
    private Task<List<string>> ExclusionsAsync(Guid userId) =>
        MealExclusions.LoadAsync(_unitOfWork, userId);
}
