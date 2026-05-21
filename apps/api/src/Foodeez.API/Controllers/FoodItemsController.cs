using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/food-items")]
public class FoodItemsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public FoodItemsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>Search food items by name or brand.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<FoodItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query is required.");

        var items = await _unitOfWork.FoodItems.SearchAsync(query);
        var dtos = items.Select(MapToDto).ToList();
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
            CreatedByUserId = request.CreatedByUserId,
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

        return CreatedAtAction(nameof(Search), new { query = foodItem.Name }, MapToDto(foodItem));
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
    public Guid? CreatedByUserId { get; set; }
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbohydrates { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
    public float Sugar { get; set; }
    public float Sodium { get; set; }
}
