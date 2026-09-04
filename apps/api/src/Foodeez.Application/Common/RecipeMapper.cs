using Foodeez.Application.DTOs.Recipes;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Common;

/// <summary>
/// The one place a <see cref="Recipe"/> becomes a <see cref="RecipeDto"/>.
///
/// It used to live on <c>SpoonacularRecipeMapper</c>, which is about importing from
/// Spoonacular and has no business owning the shape every recipe endpoint returns.
/// <c>RecipesController</c> consequently kept its own near-copy that dropped SourceUrl,
/// SourceName and HasInstructions, so an imported recipe lost its attribution depending on
/// which endpoint you fetched it from.
/// </summary>
public static class RecipeMapper
{
    public static RecipeDto ToDto(Recipe recipe) => new()
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
        SourceUrl = recipe.SourceUrl,
        SourceName = recipe.SourceName,
        HasInstructions = !string.IsNullOrWhiteSpace(recipe.Instructions),
        CreatedByUserId = recipe.CreatedByUserId,
        NutritionalInfoPerServing = NutritionMapper.ToDto(recipe.NutritionalInfoPerServing),
        Ingredients = recipe.Ingredients.Select(ToDto).ToList()
    };

    public static RecipeIngredientDto ToDto(RecipeIngredient ingredient) => new()
    {
        FoodItemId = ingredient.FoodItemId,
        FoodItemName = ingredient.FoodItem?.Name ?? ingredient.IngredientName ?? string.Empty,
        Quantity = ingredient.Quantity,
        Unit = ingredient.Unit,
        Notes = ingredient.Notes
    };
}
