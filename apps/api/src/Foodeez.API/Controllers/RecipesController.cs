using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Foodeez.API.Controllers;

[ApiController]
[Route("api/recipes")]
public class RecipesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SearchRecipesUseCase _searchUseCase;

    public RecipesController(IUnitOfWork unitOfWork, SearchRecipesUseCase searchUseCase)
    {
        _unitOfWork = unitOfWork;
        _searchUseCase = searchUseCase;
    }

    /// <summary>Search recipes by name/description with DB-first → Spoonacular fallback.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchRecipes([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query parameter 'q' is required.");

        var results = await _searchUseCase.ExecuteAsync(q, ct);
        return Ok(results);
    }

    /// <summary>Get recipes filtered by comma-separated tags.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<RecipeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecipes([FromQuery] string? tags)
    {
        IReadOnlyList<Recipe> recipes;

        if (!string.IsNullOrWhiteSpace(tags))
        {
            var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            recipes = await _unitOfWork.Recipes.GetByTagsAsync(tagList);
        }
        else
        {
            recipes = await _unitOfWork.Recipes.GetAllAsync();
        }

        return Ok(recipes.Select(MapToDto).ToList());
    }

    /// <summary>Get a specific recipe by ID including full ingredient list.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RecipeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRecipe([FromRoute] Guid id)
    {
        var recipe = await _unitOfWork.Recipes.GetByIdAsync(id);
        if (recipe == null)
            return NotFound($"Recipe with id '{id}' was not found.");

        return Ok(MapToDto(recipe));
    }

    private static RecipeDto MapToDto(Recipe recipe) => new RecipeDto
    {
        Id = recipe.Id,
        Name = recipe.Name,
        Description = recipe.Description,
        Instructions = recipe.Instructions,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Servings = recipe.Servings,
        Tags = recipe.Tags,
        ImageUrl = recipe.ImageUrl,
        IsAIGenerated = recipe.IsAIGenerated,
        CreatedByUserId = recipe.CreatedByUserId,
        NutritionalInfoPerServing = new NutritionalInfoDto
        {
            Calories = recipe.NutritionalInfoPerServing.Calories,
            Protein = recipe.NutritionalInfoPerServing.Protein,
            Carbohydrates = recipe.NutritionalInfoPerServing.Carbohydrates,
            Fat = recipe.NutritionalInfoPerServing.Fat,
            Fiber = recipe.NutritionalInfoPerServing.Fiber,
            Sugar = recipe.NutritionalInfoPerServing.Sugar,
            Sodium = recipe.NutritionalInfoPerServing.Sodium
        },
        Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientDto
        {
            FoodItemId = i.FoodItemId,
            FoodItemName = i.FoodItem?.Name ?? i.IngredientName ?? string.Empty,
            Quantity = i.Quantity,
            Unit = i.Unit,
            Notes = i.Notes
        }).ToList()
    };
}
