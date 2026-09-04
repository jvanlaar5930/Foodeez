using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace Foodeez.API.Controllers;

/// <summary>
/// Recipe discovery is deliberately anonymous: searching and reading a recipe never
/// requires an account. Saving one does, so [AllowAnonymous] is applied per action -
/// at class level it would outrank the [Authorize] on the saved-recipe routes.
/// </summary>
[ApiController]
[Route("api/recipes")]
public class RecipesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SearchRecipesUseCase _searchUseCase;
    private readonly AutocompleteRecipesUseCase _autocompleteUseCase;
    private readonly GetRecipeDetailUseCase _detailUseCase;
    private readonly SavedRecipesUseCase _savedUseCase;

    public RecipesController(
        IUnitOfWork unitOfWork,
        SearchRecipesUseCase searchUseCase,
        AutocompleteRecipesUseCase autocompleteUseCase,
        GetRecipeDetailUseCase detailUseCase,
        SavedRecipesUseCase savedUseCase)
    {
        _unitOfWork = unitOfWork;
        _searchUseCase = searchUseCase;
        _autocompleteUseCase = autocompleteUseCase;
        _detailUseCase = detailUseCase;
        _savedUseCase = savedUseCase;
    }

    /// <summary>Search-as-you-type recipe name suggestions.</summary>
    [HttpGet("autocomplete")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RecipeSuggestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Autocomplete([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new List<RecipeSuggestionDto>());

        return Ok(await _autocompleteUseCase.ExecuteAsync(q.Trim(), ct));
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
            return BadRequest("Query parameter 'q' is required.");

        return Ok(await _searchUseCase.ExecuteAsync(q, page, pageSize, ct));
    }

    /// <summary>Browse recipes, optionally filtered by comma-separated tags. Paged.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecipes(
        [FromQuery] string? tags,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = SearchRecipesUseCase.DefaultPageSize)
    {
        var size = Math.Clamp(pageSize, 1, SearchRecipesUseCase.MaxPageSize);
        var current = Math.Max(1, page);

        // Tags are stored as one comma-joined string, so there is no index to page against.
        // Fetch the matches and slice here rather than pretending the database can do it.
        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var matches = await _unitOfWork.Recipes.GetByTagsAsync(tagList);

            return Ok(new PagedResult<RecipeDto>
            {
                Items = matches.Skip((current - 1) * size).Take(size).Select(RecipeMapper.ToDto).ToList(),
                Page = current,
                PageSize = size,
                HasMore = matches.Count > current * size,
                TotalAvailable = matches.Count,
            });
        }

        return Ok(await _searchUseCase.BrowseAsync(current, size));
    }

    // -- Saved recipes --------------------------------------------------------
    // The only authenticated routes on this controller. The literal segment "saved" can
    // never parse as a Guid, so it does not collide with GET {id:guid}.

    /// <summary>The signed-in user's saved recipes, most recently saved first.</summary>
    [HttpGet("saved")]
    [Authorize]
    [ProducesResponseType(typeof(List<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedRecipes(CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();
        return Ok(await _savedUseCase.ListAsync(userId, ct));
    }

    /// <summary>Save a recipe to the signed-in user's collection. Idempotent.</summary>
    [HttpPut("{id:guid}/save")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveRecipe([FromRoute] Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();

        return await _savedUseCase.SaveAsync(userId, id, ct)
            ? NoContent()
            : NotFound($"Recipe with id '{id}' was not found.");
    }

    /// <summary>Remove a recipe from the signed-in user's collection. Idempotent.</summary>
    [HttpDelete("{id:guid}/save")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnsaveRecipe([FromRoute] Guid id, CancellationToken ct)
    {
        if (CurrentUserId() is not { } userId) return Unauthorized();

        await _savedUseCase.UnsaveAsync(userId, id, ct);
        return NoContent();
    }

    /// <summary>The authenticated user's id, from the token's subject claim.</summary>
    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
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
