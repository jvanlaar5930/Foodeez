using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.FoodItems;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/food-items")]
public class FoodItemsController : ControllerBase
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
            return BadRequest("Search query is required.");

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
            CreatedByUserId = CurrentUserId(),
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

        return CreatedAtAction(nameof(Search), new { q = foodItem.Name }, MapToDto(foodItem));
    }

    /// <summary>The authenticated user's id, from the token's subject claim.</summary>
    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static FoodItemDto MapToDto(FoodItem item) => new FoodItemDto
    {
        Id = item.Id,
        Name = item.Name,
        Brand = item.Brand,
        ServingSize = item.ServingSize,
        ServingUnit = item.ServingUnit,
        Category = item.Category,
        NutritionalInfo = new NutritionalInfoDto
        {
            Calories = item.NutritionalInfo.Calories,
            Protein = item.NutritionalInfo.Protein,
            Carbohydrates = item.NutritionalInfo.Carbohydrates,
            Fat = item.NutritionalInfo.Fat,
            Fiber = item.NutritionalInfo.Fiber,
            Sugar = item.NutritionalInfo.Sugar,
            Sodium = item.NutritionalInfo.Sodium
        }
    };
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
