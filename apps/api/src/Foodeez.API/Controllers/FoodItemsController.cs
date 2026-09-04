using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.FoodItems;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Foodeez.API.Controllers;

[Route("api/food-items")]
public class FoodItemsController : FoodeezController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SearchFoodItemsUseCase _searchUseCase;

    public FoodItemsController(IUnitOfWork unitOfWork, SearchFoodItemsUseCase searchUseCase)
    {
        _unitOfWork = unitOfWork;
        _searchUseCase = searchUseCase;
    }

    /// <summary>Search food items by name or brand (DB-first, falls back to USDA FoodData Central).</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<FoodItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(Failure("Search query is required."));

        var dtos = await _searchUseCase.ExecuteAsync(q, ct);
        return Ok(dtos);
    }

    /// <summary>Create a custom food item.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(FoodItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFoodItem([FromBody] CreateFoodItemRequest request)
    {
        var foodItem = new FoodItem
        {
            Name = request.Name,
            Brand = request.Brand,
            ServingSize = request.ServingSize,
            ServingUnit = request.ServingUnit,
            Category = request.Category,
            Barcode = request.Barcode,
            IsCustom = true,
            // Ownership comes from the token, never the request body: a client should not be
            // able to file its custom foods under somebody else's account.
            CreatedByUserId = UserIdOrNull,
            NutritionalInfo = new NutritionalInfo(
                request.Calories,
                request.Protein,
                request.Carbohydrates,
                request.Fat,
                request.Fiber,
                request.Sugar,
                request.Sodium)
        };

        await _unitOfWork.FoodItems.AddAsync(foodItem);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(Search), new { q = foodItem.Name }, FoodItemMapper.ToDto(foodItem));
    }


}

public class CreateFoodItemRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public float ServingSize { get; set; }
    public string ServingUnit { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}
