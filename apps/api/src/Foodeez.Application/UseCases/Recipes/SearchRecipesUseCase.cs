using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// DB-first recipe search, falling back to Spoonacular and caching what comes back.
///
/// Note that `complexSearch` returns no ingredients and no instructions whatsoever, so the
/// rows cached here are deliberately partial - enough for a result card. The full recipe is
/// filled in lazily by <see cref="GetRecipeDetailUseCase"/> when someone actually opens one,
/// which keeps the per-recipe detail calls off the search path.
/// </summary>
public class SearchRecipesUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpoonacularService _spoonacular;

    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    public SearchRecipesUseCase(IUnitOfWork unitOfWork, ISpoonacularService spoonacular)
    {
        _unitOfWork = unitOfWork;
        _spoonacular = spoonacular;
    }

    /// <summary>
    /// One page of matches. Paging maps onto the upstream API's own offset, so scrolling to
    /// page N costs one upstream call rather than refetching everything before it - and a
    /// user who never scrolls past the first ten results costs exactly one.
    /// </summary>
    public async Task<PagedResult<RecipeDto>> ExecuteAsync(
        string query,
        int page = 1,
        int pageSize = DefaultPageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var skip = (page - 1) * pageSize;

        // Over-fetch by one: if the extra row exists there is another page, and we learn that
        // without paying for a separate COUNT.
        var dbMatches = await _unitOfWork.Recipes.SearchPagedAsync(query, skip, pageSize + 1);
        var dbHasMore = dbMatches.Count > pageSize;
        var dbPage = dbMatches.Take(pageSize).ToList();

        // A full local page is enough on its own; no reason to spend an upstream call.
        if (dbPage.Count == pageSize)
        {
            return new PagedResult<RecipeDto>
            {
                Items = dbPage.Select(SpoonacularRecipeMapper.MapToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                HasMore = true,
            };
        }

        // Top the page up from upstream, offsetting by however many local rows already filled it.
        var wanted = pageSize - dbPage.Count;
        var upstream = await _spoonacular.SearchRecipesPagedAsync(query, skip, wanted + 1, ct);

        if (upstream.Results.Count == 0)
        {
            return new PagedResult<RecipeDto>
            {
                Items = dbPage.Select(SpoonacularRecipeMapper.MapToDto).ToList(),
                Page = page,
                PageSize = pageSize,
                HasMore = dbHasMore,
            };
        }

        var upstreamHasMore = upstream.Results.Count > wanted || skip + pageSize < upstream.TotalResults;
        var ranked = await CacheAndRankAsync(upstream.Results.Take(wanted).ToList(), ct);

        // Local text matches first, then upstream's relevance order, deduped.
        var items = new List<Recipe>(dbPage);
        var seen = dbPage.Select(r => r.Id).ToHashSet();
        foreach (var recipe in ranked)
        {
            if (seen.Add(recipe.Id)) items.Add(recipe);
        }

        return new PagedResult<RecipeDto>
        {
            Items = items.Select(SpoonacularRecipeMapper.MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            HasMore = dbHasMore || upstreamHasMore,
            TotalAvailable = upstream.TotalResults > 0 ? upstream.TotalResults : null,
        };
    }

    /// <summary>Persist anything new and return the batch in the order the upstream ranked it.</summary>
    private async Task<List<Recipe>> CacheAndRankAsync(List<SpoonacularRecipeResult> results, CancellationToken ct)
    {
        var existingBySpoon = await _unitOfWork.Recipes.GetBySpoonacularIdsAsync(results.Select(r => r.Id));
        var existingIdMap = existingBySpoon.ToDictionary(r => r.SpoonacularId!.Value);

        var now = DateTime.UtcNow;
        var toAdd = new List<Recipe>();
        var ranked = new List<Recipe>(results.Count);

        foreach (var sr in results)
        {
            if (existingIdMap.TryGetValue(sr.Id, out var existing))
            {
                if (existing.SpoonacularSyncedAt.HasValue && now - existing.SpoonacularSyncedAt.Value > StaleThreshold)
                    SpoonacularRecipeMapper.ApplySpoonacularUpdate(existing, sr, now);
                ranked.Add(existing);
                continue;
            }

            var mapped = SpoonacularRecipeMapper.MapToEntity(sr, now);
            toAdd.Add(mapped);
            ranked.Add(mapped);
        }

        foreach (var recipe in toAdd)
            await _unitOfWork.Recipes.AddAsync(recipe);

        if (toAdd.Count > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return ranked;
    }

    /// <summary>
    /// Browse with no query: purely local, since there is nothing to ask upstream for.
    /// Optionally narrowed to recipes carrying all of the given tags.
    /// </summary>
    public async Task<PagedResult<RecipeDto>> BrowseAsync(
        string? tags = null, int page = 1, int pageSize = DefaultPageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var tagList = ParseTags(tags);
        return tagList.Count > 0
            ? await BrowseByTagsAsync(tagList, page, pageSize)
            : await BrowsePageAsync(page, pageSize);
    }

    /// <summary>Tags arrive as one comma-separated query parameter.</summary>
    private static List<string> ParseTags(string? tags) =>
        string.IsNullOrWhiteSpace(tags)
            ? []
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private async Task<PagedResult<RecipeDto>> BrowsePageAsync(int page, int pageSize)
    {
        // One row past the page, so "is there a next page" is answered without a count query.
        var rows = await _unitOfWork.Recipes.GetPagedAsync((page - 1) * pageSize, pageSize + 1);

        return new PagedResult<RecipeDto>
        {
            Items = rows.Take(pageSize).Select(RecipeMapper.ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            HasMore = rows.Count > pageSize,
        };
    }

    /// <summary>
    /// Tags are stored as one comma-joined string, so there is no index to page against.
    /// The matches are fetched and sliced here rather than pretending the database can do it -
    /// which is also why this reports TotalAvailable and the untagged page above does not.
    /// </summary>
    private async Task<PagedResult<RecipeDto>> BrowseByTagsAsync(List<string> tags, int page, int pageSize)
    {
        var matches = await _unitOfWork.Recipes.GetByTagsAsync(tags);

        return new PagedResult<RecipeDto>
        {
            Items = matches.Skip((page - 1) * pageSize).Take(pageSize).Select(RecipeMapper.ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            HasMore = matches.Count > page * pageSize,
            TotalAvailable = matches.Count,
        };
    }
}
