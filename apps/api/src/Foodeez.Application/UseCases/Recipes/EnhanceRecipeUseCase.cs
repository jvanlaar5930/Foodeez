using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// The elevated version of a recipe: the same dish as a restaurant kitchen would cook it,
/// kept beside the original rather than replacing it.
///
/// Two rules shape everything here. The original is never touched - it is a library recipe
/// other people are reading, and the person asking for a chef's take has not asked to rewrite
/// it for everyone. And there is exactly one enhancement per recipe per person: asking again
/// hands back the one already written, and only an explicit refresh spends a second call on
/// the model, rewriting that same row. Without that, every reopened recipe would quietly cost
/// another generation and leave another near-identical copy behind.
/// </summary>
public class EnhanceRecipeUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _ai;
    private readonly GetRecipeDetailUseCase _detailUseCase;

    public EnhanceRecipeUseCase(IUnitOfWork unitOfWork, IAIService ai, GetRecipeDetailUseCase detailUseCase)
    {
        _unitOfWork = unitOfWork;
        _ai = ai;
        _detailUseCase = detailUseCase;
    }

    /// <summary>
    /// This person's enhancement of a recipe, or null if they have never asked for one.
    /// Never generates: opening a recipe must not cost an AI call.
    /// </summary>
    public async Task<RecipeDto?> GetAsync(Guid recipeId, Guid userId, CancellationToken ct = default)
    {
        var enhancement = await _unitOfWork.Recipes.GetEnhancementAsync(recipeId, userId, ct);
        return enhancement == null ? null : RecipeMapper.ToDto(enhancement);
    }

    /// <summary>
    /// Writes this person's enhancement of a recipe and returns it.
    /// </summary>
    /// <param name="refresh">
    /// True when the reader has seen the enhancement and wants a different take. Without it an
    /// existing enhancement is returned untouched, so a second click - or a second device -
    /// costs nothing and cannot produce a third version of the same dish.
    /// </param>
    /// <exception cref="KeyNotFoundException">The recipe does not exist.</exception>
    /// <exception cref="InvalidOperationException">The recipe is itself an enhancement.</exception>
    /// <exception cref="AIGenerationFailedException">The provider could not write one.</exception>
    public async Task<RecipeDto> ExecuteAsync(
        Guid recipeId, Guid userId, bool refresh = false, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.Recipes.GetEnhancementAsync(recipeId, userId, ct);
        if (existing != null && !refresh)
        {
            return RecipeMapper.ToDto(existing);
        }

        // Through the detail use case rather than the repository: a recipe cached from a
        // search carries no method at all, and asking a chef to improve an empty page gets
        // an invented dish back. This fills it in first, exactly as opening the recipe does.
        var original = await _detailUseCase.ExecuteAsync(recipeId, ct)
            ?? throw new KeyNotFoundException($"Recipe with id '{recipeId}' was not found.");

        if (original.IsEnhanced)
        {
            throw new InvalidOperationException(
                "This recipe is already an enhanced version. Enhance the original recipe instead, " +
                "or refresh this enhancement.");
        }

        var enhanced = await _ai.EnhanceRecipeAsync(
            new EnhanceRecipeRequest
            {
                Original = original,
                // From the profile, never from the caller: a client that forgot to send these
                // would otherwise get a dish built on somebody's allergy.
                ExcludedFoods = await MealExclusions.LoadAsync(_unitOfWork, userId),
                PreviousEnhancement = existing == null ? null : Describe(existing)
            },
            ct);

        if (!enhanced.Succeeded || string.IsNullOrWhiteSpace(enhanced.Instructions))
        {
            // Nothing is written. A reader who already had an enhancement still has it, which
            // is the whole reason a failed refresh must not touch the row.
            throw new AIGenerationFailedException(
                "The AI service could not enhance this recipe right now. Please try again in a moment.");
        }

        var recipe = existing ?? NewEnhancement(original, userId);
        Apply(recipe, original, enhanced);

        var ingredients = BuildIngredients(recipe.Id, enhanced);

        if (existing == null)
        {
            foreach (var ingredient in ingredients)
            {
                recipe.Ingredients.Add(ingredient);
            }

            await _unitOfWork.Recipes.AddAsync(recipe);
        }
        else
        {
            // The tracked-parent case: a freshly built ingredient already carries a key, which
            // the change tracker would read as an existing row and try to UPDATE.
            _unitOfWork.Recipes.ReplaceIngredients(recipe, ingredients);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return RecipeMapper.ToDto(recipe);
    }

    private static Recipe NewEnhancement(RecipeDto original, Guid userId) => new()
    {
        EnhancedFromRecipeId = original.Id,
        CreatedByUserId = userId,
        // Generated content, and marked as such: that is what keeps it out of everyone else's
        // library, on the same rule that hides a generated meal plan's recipes.
        IsAIGenerated = true,
        // The dish has not changed, so its photograph has not either.
        ImageUrl = original.ImageUrl,
        // Deliberately not the original's attribution. The method below is not the one that
        // publisher wrote, and printing their name over it would credit them for a rewrite
        // they had no part in. The original keeps its own attribution, one toggle away.
        SourceName = "Foodeez AI"
    };

    /// <summary>
    /// Writes the model's version onto the row, keeping the original's name.
    ///
    /// The name is the one thing that does not change. This is the same recipe elevated, not a
    /// new dish, and a reader toggling between two versions of their dinner needs the title to
    /// still say which dinner it is - a menu-style rename reads as having lost the recipe.
    /// </summary>
    private static void Apply(Recipe recipe, RecipeDto original, EnhancedRecipeDto enhanced)
    {
        recipe.Name = original.Name;
        recipe.Description = enhanced.Description ?? original.Description;
        recipe.Instructions = enhanced.Instructions.Trim();

        // A model that left a timing out gets the original's rather than zero, which would
        // read on screen as a dish that takes no time to cook.
        recipe.PrepTimeMinutes = enhanced.PrepTimeMinutes > 0 ? enhanced.PrepTimeMinutes : original.PrepTimeMinutes;
        recipe.CookTimeMinutes = enhanced.CookTimeMinutes > 0 ? enhanced.CookTimeMinutes : original.CookTimeMinutes;
        recipe.Servings = enhanced.Servings > 0 ? enhanced.Servings : Math.Max(1, original.Servings);

        recipe.Tags = RecipeTags.Join(RecipeTags.Split(original.Tags).Concat(enhanced.Tags));

        recipe.EnhancementNotes = enhanced.ChefNotes.Count > 0
            ? string.Join("\n", enhanced.ChefNotes.Select(note => note.Trim()).Where(note => note.Length > 0))
            : null;

        recipe.EnhancedAt = DateTime.UtcNow;

        // The dish changed, so its numbers did too - but only if the model actually costed it.
        // Zero calories on a plate of food is a missing answer, not a light supper.
        recipe.NutritionalInfoPerServing = NutritionMapper.ToDomain(
            enhanced.NutritionalInfoPerServing.Calories > 0
                ? enhanced.NutritionalInfoPerServing
                : original.NutritionalInfoPerServing);
    }

    private static List<RecipeIngredient> BuildIngredients(Guid recipeId, EnhancedRecipeDto enhanced) =>
        enhanced.Ingredients
            .Where(ingredient => !string.IsNullOrWhiteSpace(ingredient.Name))
            .Select(ingredient => new RecipeIngredient
            {
                RecipeId = recipeId,
                // Free text, like a generated plan's: these are the chef's amounts, and
                // matching each one to a food item is a different job with its own failures.
                IngredientName = ingredient.Name.Trim(),
                Quantity = ingredient.Quantity,
                Unit = ingredient.Unit,
                Notes = ingredient.Notes
            })
            .ToList();

    /// <summary>
    /// The enhancement being replaced, written out for the prompt. Enough for the model to
    /// recognise what it already suggested and go somewhere else, without shipping the whole
    /// row back to it.
    /// </summary>
    private static string Describe(Recipe existing)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(existing.Description))
        {
            parts.Add(existing.Description.Trim());
        }

        if (!string.IsNullOrWhiteSpace(existing.EnhancementNotes))
        {
            parts.Add(existing.EnhancementNotes.Trim());
        }

        if (!string.IsNullOrWhiteSpace(existing.Instructions))
        {
            parts.Add(existing.Instructions.Trim());
        }

        return string.Join("\n", parts);
    }
}
