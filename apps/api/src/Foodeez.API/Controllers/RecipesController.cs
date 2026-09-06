using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Foodeez.API.Controllers;

/// <summary>
/// Recipe discovery is deliberately anonymous: searching and reading a recipe never
/// requires an account. Saving one does, so [AllowAnonymous] is applied per action -
/// at class level it would outrank the [Authorize] on the saved-recipe routes.
/// </summary>
[Route("api/recipes")]
public class RecipesController : FoodeezController
{
    private readonly SearchRecipesUseCase _searchUseCase;
    private readonly AutocompleteRecipesUseCase _autocompleteUseCase;
    private readonly GetRecipeDetailUseCase _detailUseCase;
    private readonly SavedRecipesUseCase _savedUseCase;
    private readonly RecipeFilterTagsUseCase _filterTagsUseCase;

    public RecipesController(
        SearchRecipesUseCase searchUseCase,
        AutocompleteRecipesUseCase autocompleteUseCase,
        GetRecipeDetailUseCase detailUseCase,
        SavedRecipesUseCase savedUseCase,
        RecipeFilterTagsUseCase filterTagsUseCase)
    {
        _searchUseCase = searchUseCase;
        _autocompleteUseCase = autocompleteUseCase;
        _detailUseCase = detailUseCase;
        _savedUseCase = savedUseCase;
        _filterTagsUseCase = filterTagsUseCase;
    }

    /// <summary>The filter pills the clients show above their results, in the admin's order.</summary>
    [HttpGet("filter-tags")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilterTags() => Ok(await _filterTagsUseCase.ExecuteAsync());

    /// <summary>Search-as-you-type recipe name suggestions.</summary>
    [HttpGet("autocomplete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RecipeSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autocomplete([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new List<RecipeSuggestionDto>());

        return Ok(await _autocompleteUseCase.ExecuteAsync(q.Trim(), UserIdOrNull, ct));
    }

    /// <summary>Search recipes by name/description with DB-first → Spoonacular fallback.</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchRecipes(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = SearchRecipesUseCase.DefaultPageSize,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(Failure("Query parameter 'q' is required."));

        return Ok(await _searchUseCase.ExecuteAsync(q, UserIdOrNull, page, pageSize, ct));
    }

    /// <summary>
    /// Browse recipes, filtered and paged by the database.
    ///
    /// `previousMeals` and `favorites` are the two filters that are not tags: one asks who
    /// wrote the recipe, the other whether this caller kept it. Both need to know who is
    /// asking, and both answer with nothing at all when nobody is signed in.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecipes(
        [FromQuery] string? tags,
        [FromQuery] bool previousMeals = false,
        [FromQuery] bool favorites = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = SearchRecipesUseCase.DefaultPageSize) =>
        Ok(await _searchUseCase.BrowseAsync(
            new RecipeBrowseFilter
            {
                Tags = SearchRecipesUseCase.ParseTags(tags),
                ViewerId = UserIdOrNull,
                OnlyPreviousMeals = previousMeals,
                OnlyFavorites = favorites,
            },
            page,
            pageSize));

    // -- Saved recipes --------------------------------------------------------
    // The only authenticated routes on this controller. The literal segment "saved" can
    // never parse as a Guid, so it does not collide with GET {id:guid}.

    /// <summary>The signed-in user's saved recipes, most recently saved first.</summary>
    [HttpGet("saved")]
    [Authorize]
    [ProducesResponseType(typeof(List<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedRecipes(CancellationToken ct)
    {
        return Ok(await _savedUseCase.ListAsync(UserId, ct));
    }

    /// <summary>Save a recipe to the signed-in user's collection. Idempotent.</summary>
    [HttpPut("{id:guid}/save")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveRecipe([FromRoute] Guid id, CancellationToken ct)
    {
        return await _savedUseCase.SaveAsync(UserId, id, ct)
            ? NoContent()
            : NotFound($"Recipe with id '{id}' was not found.");
    }

    /// <summary>Remove a recipe from the signed-in user's collection. Idempotent.</summary>
    [HttpDelete("{id:guid}/save")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveRecipe([FromRoute] Guid id, CancellationToken ct)
    {
        await _savedUseCase.UnsaveAsync(UserId, id, ct);
        return NoContent();
    }


    /// <summary>Get a specific recipe by ID, including the full ingredient list and steps.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RecipeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipe([FromRoute] Guid id, CancellationToken ct)
    {
        var recipe = await _detailUseCase.ExecuteAsync(id, ct);
        if (recipe == null)
            return NotFound($"Recipe with id '{id}' was not found.");

        return Ok(recipe);
    }

}
