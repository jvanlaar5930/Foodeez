using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Chat;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.UseCases.Recipes;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.Chat;

/// <summary>Why keeping a reply's recipes did or did not put them in the library.</summary>
public enum SaveRecipesOutcome
{
    Saved,
    NotFound,
    NothingToSave
}

public sealed record SaveRecipesResult(SaveRecipesOutcome Outcome, List<RecipeDto> Recipes);

/// <summary>
/// Takes a recipe the assistant wrote out and puts it in the recipe library.
///
/// The library is shared across every account and is what recipe search reads first, so a
/// recipe only lands there when someone asks for it - otherwise a conversation idly listing
/// four ways to cook a chicken breast would leave four rows behind for everyone. Keeping one
/// also bookmarks it for the person who kept it, since a recipe they cannot find again in
/// their own collection has not really been saved.
/// </summary>
public class SaveChatRecipesUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public SaveChatRecipesUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SaveRecipesResult> ExecuteAsync(
        Guid userId, Guid messageId, IReadOnlyList<int> indexes, CancellationToken ct = default)
    {
        var message = await _unitOfWork.Chat.GetMessageAsync(messageId);
        if (message == null || message.Conversation.UserId != userId)
        {
            return new SaveRecipesResult(SaveRecipesOutcome.NotFound, new List<RecipeDto>());
        }

        var chosen = Select(message.Recipes, indexes);
        if (chosen.Count == 0)
        {
            return new SaveRecipesResult(SaveRecipesOutcome.NothingToSave, new List<RecipeDto>());
        }

        var saved = new List<Recipe>();
        foreach (var suggestion in chosen)
        {
            saved.Add(await KeepAsync(userId, suggestion, ct));
        }

        message.RecipesSavedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);

        return new SaveRecipesResult(
            SaveRecipesOutcome.Saved,
            saved.Select(SpoonacularRecipeMapper.MapToDto).ToList());
    }

    /// <summary>
    /// The library row for one suggestion, reusing the one this person already kept under
    /// the same name if there is one. Asking twice - a thread reopened, a repeated answer -
    /// should leave one recipe in the collection, not a pile of identical ones.
    /// </summary>
    private async Task<Recipe> KeepAsync(Guid userId, SuggestedRecipe suggestion, CancellationToken ct)
    {
        var existing = await FindOwnAsync(userId, suggestion.Name);
        if (existing != null)
        {
            await BookmarkAsync(userId, existing.Id, ct);
            return existing;
        }

        var recipe = new Recipe
        {
            Name = suggestion.Name,
            Description = suggestion.Description,
            Instructions = suggestion.Instructions,
            PrepTimeMinutes = suggestion.PrepTimeMinutes,
            CookTimeMinutes = suggestion.CookTimeMinutes,
            Servings = suggestion.Servings > 0 ? suggestion.Servings : 1,
            Tags = suggestion.Tags,
            ImageUrl = AiRecipeImage.Marker,
            IsAIGenerated = true,
            CreatedByUserId = userId,
            SourceName = "Foodeez AI",
            NutritionalInfoPerServing = new NutritionalInfo(
                suggestion.Calories,
                suggestion.Protein,
                suggestion.Carbohydrates,
                suggestion.Fat,
                suggestion.Fiber,
                suggestion.Sugar,
                suggestion.Sodium)
        };

        foreach (var ingredient in suggestion.Ingredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                RecipeId = recipe.Id,
                IngredientName = ingredient.Name,
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit,
                Notes = ingredient.Notes
            });
        }

        await _unitOfWork.Recipes.AddAsync(recipe);
        await _unitOfWork.SaveChangesAsync(ct);

        await BookmarkAsync(userId, recipe.Id, ct);

        return recipe;
    }

    /// <summary>A recipe this user already kept from the assistant under this exact name.</summary>
    private async Task<Recipe?> FindOwnAsync(Guid userId, string name)
    {
        var matches = await _unitOfWork.Recipes.SearchAsync(name);

        return matches.FirstOrDefault(r =>
            r.CreatedByUserId == userId
            && string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task BookmarkAsync(Guid userId, Guid recipeId, CancellationToken ct)
    {
        if (await _unitOfWork.SavedRecipes.FindAsync(userId, recipeId, ct) is not null)
        {
            return;
        }

        await _unitOfWork.SavedRecipes.AddAsync(new SavedRecipe
        {
            UserId = userId,
            RecipeId = recipeId,
            SavedAt = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <summary>
    /// The recipes to keep. No indexes means all of them, which is what a single "Save"
    /// button sends; out-of-range indexes are ignored rather than failing, since a stale
    /// client asking for a recipe that is no longer there still wanted the rest.
    /// </summary>
    private static List<SuggestedRecipe> Select(
        List<SuggestedRecipe> recipes, IReadOnlyList<int> indexes)
    {
        if (indexes.Count == 0)
        {
            return recipes;
        }

        return indexes
            .Where(i => i >= 0 && i < recipes.Count)
            .Distinct()
            .Select(i => recipes[i])
            .ToList();
    }
}
