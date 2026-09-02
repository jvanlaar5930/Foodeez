using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.MealLogs;

/// <summary>
/// Turns the foods a model read out of a meal into rows that can actually be logged.
///
/// The model is not trusted with the numbers. Every name is looked up in the food database
/// first, and a real match wins - so "2 slices of whole wheat bread" is counted from the
/// bread already on file rather than from whatever the model remembered about bread. Only a
/// food nothing knows about falls back to the estimate, and that line is marked as an
/// estimate so the person logging it knows which figures to glance at.
/// </summary>
public class ParsedMealResolver
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>How many candidates to weigh per parsed food. Enough to find the right one, few enough to stay quick.</summary>
    private const int CandidateLimit = 10;

    public ParsedMealResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<QuickAddResultDto> ResolveAsync(Guid userId, ParsedMealDto parsed, CancellationToken ct = default)
    {
        var result = new QuickAddResultDto { Note = parsed.Note };

        foreach (var item in parsed.Items)
        {
            var quantity = item.ServingSize > 0 ? item.ServingSize : 1f;
            var matched = await FindMatchAsync(userId, item);

            if (matched is not null)
            {
                result.Items.Add(new QuickAddItemDto
                {
                    FoodItem = FoodItemMapper.ToDto(matched),
                    Quantity = quantity,
                    Unit = matched.ServingUnit,
                    Source = QuickAddSource.Matched,
                    Confidence = item.Confidence
                });
                continue;
            }

            var estimated = await ReuseOrCreateEstimateAsync(userId, item, quantity);
            result.Items.Add(new QuickAddItemDto
            {
                FoodItem = FoodItemMapper.ToDto(estimated),
                Quantity = quantity,
                Unit = estimated.ServingUnit,
                Source = QuickAddSource.Estimated,
                Confidence = item.Confidence
            });
        }

        // The estimated foods created above are only worth keeping if they are about to be
        // referenced, and the client references them by id, so they are saved here.
        if (result.Items.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return result;
    }

    /// <summary>
    /// The food already on file that this parsed line is talking about, or null when nothing
    /// on file is a safe enough answer.
    /// </summary>
    private async Task<FoodItem?> FindMatchAsync(Guid userId, ParsedFoodDto item)
    {
        // Deliberately the database only, not the USDA-backed search: a six-item meal would
        // otherwise be six upstream lookups before the user sees anything. USDA results are
        // written into the database as people search for them, so this improves with use.
        var candidates = await _unitOfWork.FoodItems.SearchAsync(item.Name, CandidateLimit);
        if (candidates.Count == 0)
        {
            return null;
        }

        var wanted = NormalizeName(item.Name);

        return candidates
            // Somebody else's custom food is not a public fact about that food, and quick add
            // reaches for these by itself rather than being asked to. Shared foods and the
            // user's own are fair game; another person's "Dad's chili" is not.
            .Where(c => !c.IsCustom || c.CreatedByUserId == userId)
            // A match in a unit that cannot be read as the one described is not a match: the
            // amount would be right and the food wrong, which is worse than no answer at all.
            .Where(c => FoodUnits.AreInterchangeable(c.ServingUnit, item.ServingUnit))
            .Select(c => new { Item = c, Score = ScoreName(NormalizeName(c.Name), wanted) })
            .Where(c => c.Score > 0)
            .OrderByDescending(c => c.Score)
            // Between equally good names, prefer the shorter: "Bread" beats "Bread pudding
            // mix, dry" for a line that just said bread.
            .ThenBy(c => c.Item.Name.Length)
            .Select(c => c.Item)
            .FirstOrDefault();
    }

    /// <summary>
    /// How well a food's name answers the one that was described. 0 means it does not.
    /// </summary>
    private static int ScoreName(string candidate, string wanted)
    {
        if (candidate == wanted)
        {
            return 3;
        }

        // "Turkey breast, sliced" answers "turkey breast"; "turkey" does not, on its own, but
        // a line that only said "turkey" is answered by it.
        if (candidate.StartsWith(wanted, StringComparison.Ordinal) || wanted.StartsWith(candidate, StringComparison.Ordinal))
        {
            return 2;
        }

        return candidate.Contains(wanted, StringComparison.Ordinal) ? 1 : 0;
    }

    private static string NormalizeName(string name) =>
        new string(name.Trim().ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray())
            .Replace("  ", " ");

    /// <summary>
    /// The user's own record for an estimated food, made once and reused after that.
    ///
    /// Reuse keeps a repeated meal stable: the second turkey sandwich is counted with exactly
    /// the numbers the first one was, instead of drifting every time the model is asked again.
    /// It also stops a new food item being filed on every single breakfast.
    /// </summary>
    private async Task<FoodItem> ReuseOrCreateEstimateAsync(Guid userId, ParsedFoodDto item, float quantity)
    {
        var existing = await _unitOfWork.FoodItems.FindCustomByNameAsync(userId, item.Name, item.ServingUnit);
        if (existing is not null)
        {
            return existing;
        }

        var created = new FoodItem
        {
            Name = item.Name,
            Brand = item.Brand,
            // The parsed line's amount becomes this food's serving, so its nutrition - which
            // covers exactly that amount - needs no scaling to be right.
            ServingSize = quantity,
            ServingUnit = item.ServingUnit,
            IsCustom = true,
            CreatedByUserId = userId,
            NutritionalInfo = new NutritionalInfo(
                item.NutritionalInfo.Calories,
                item.NutritionalInfo.Protein,
                item.NutritionalInfo.Carbohydrates,
                item.NutritionalInfo.Fat,
                item.NutritionalInfo.Fiber,
                item.NutritionalInfo.Sugar,
                item.NutritionalInfo.Sodium)
        };

        await _unitOfWork.FoodItems.AddAsync(created);
        return created;
    }
}
