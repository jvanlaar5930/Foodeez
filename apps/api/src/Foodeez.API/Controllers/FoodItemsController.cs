using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.UseCases.FoodItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[Route("api/food-items")]
public class FoodItemsController : FoodeezController
{
    private readonly SearchFoodItemsUseCase _searchUseCase;
    private readonly CreateFoodItemUseCase _createUseCase;

    public FoodItemsController(SearchFoodItemsUseCase searchUseCase, CreateFoodItemUseCase createUseCase)
    {
        _searchUseCase = searchUseCase;
        _createUseCase = createUseCase;
    }

    /// <summary>Search food items by name or brand (DB-first, falls back to USDA FoodData Central).</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<FoodItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(Failure("Search query is required."));

        return Ok(await _searchUseCase.ExecuteAsync(q, ct));
    }

    /// <summary>Create a custom food item, owned by the signed-in user.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(FoodItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFoodItem([FromBody] CreateFoodItemRequest request)
    {
        var created = await _createUseCase.ExecuteAsync(request, UserId);

        return CreatedAtAction(nameof(Search), new { q = created.Name }, created);
    }
}
