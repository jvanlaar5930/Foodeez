using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.UseCases.Grocery;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Foodeez.Application.UseCases.MealPlans;

public class GenerateAIMealPlanUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStreamingAIService _streaming;
    private readonly PlannedMealReader _plannedMeals;
    private readonly ILogger<GenerateAIMealPlanUseCase> _logger;

    public GenerateAIMealPlanUseCase(
        IUnitOfWork unitOfWork,
        IStreamingAIService streaming,
        PlannedMealReader plannedMeals,
        ILogger<GenerateAIMealPlanUseCase> logger)
    {
        _unitOfWork = unitOfWork;
        _streaming = streaming;
        _plannedMeals = plannedMeals;
        _logger = logger;
    }

    public async Task<MealPlanDto> ExecuteAsync(GenerateMealPlanRequest request, CancellationToken ct = default)
    {
        var profileDto = await LoadProfileAsync(request.UserId);
        await AttachPreviousPeriodAsync(request);

        // Built from MealPlanPrompt, exactly as the streaming path below does, rather than
        // delegated to each provider's own prompt builder. Only Claude's mentioned
        // ExcludeIngredients, so on every other provider this path planned meals around foods
        // the user had told us they cannot eat; the per-provider parsers also dropped
        // ingredients and instructions. One prompt and one parser means one behaviour.
        var transcript = new StringBuilder();
        await foreach (var chunk in _streaming.StreamAsync(BuildPrompt(request, profileDto), ct))
        {
            transcript.Append(chunk);
        }

        var generated = MealPlanPrompt.Parse(transcript.ToString());

        // Providers swallow their own transport errors and hand back an empty result, so an
        // outage arrives here looking exactly like a plan with no days in it. Saving that
        // would hand the user a persisted, permanently blank week and report success -
        // refusing is the only honest answer, and it leaves nothing to clean up.
        if (generated.Days.Count == 0)
        {
            _logger.LogWarning(
                "AI meal plan produced no usable days. Raw response ({Length} chars): {Response}",
                transcript.Length, Truncate(transcript.ToString(), 4000));

            throw new AIGenerationFailedException(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");
        }

        return await PersistAsync(request, generated);
    }

    /// <summary>
    /// The same generation, streamed as the model writes it: the plan's rationale reaches the
    /// screen while the week is still being written, and the plan is saved once it is whole.
    /// </summary>
    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        GenerateMealPlanRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var profileDto = await LoadProfileAsync(request.UserId);
        await AttachPreviousPeriodAsync(request);

        var transcript = new StringBuilder();
        await foreach (var delta in AINarration.NarrateAsync(
            _streaming, BuildPrompt(request, profileDto), transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var generated = MealPlanPrompt.Parse(transcript.ToString());
        if (generated.Days.Count == 0)
        {
            // Same judgement as the blocking path: a plan with no days is an outage, not a
            // plan, and saving it would hand the user a permanently blank week. Logging the
            // raw text is what turns "it failed" into an actual diagnosis - a truncated
            // response, a model that ignored the JSON-only instruction, and a genuine
            // provider outage all reach here identically otherwise.
            _logger.LogWarning(
                "AI meal plan stream produced no usable days. Raw response ({Length} chars): {Response}",
                transcript.Length, Truncate(transcript.ToString(), 4000));

            yield return AIStreamEvent.Error(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");
            yield break;
        }

        yield return AIStreamEvent.Result(await PersistAsync(request, generated));
    }

    /// <summary>
    /// The one prompt both paths use. Exclusions are folded in here so that no route to the
    /// model can lose them - a food someone is allergic to must not depend on which endpoint
    /// the client happened to call.
    /// </summary>
    private static string BuildPrompt(GenerateMealPlanRequest request, UserProfileDto profile) =>
        MealPlanPrompt.Build(WithProfileExclusions(request, profile), profile);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    /// <summary>
    /// Tags are stored as one comma-separated string, capped at the column's 500 characters.
    /// Whole tags are dropped rather than the string being cut mid-word, which would leave a
    /// corrupted last tag behind.
    /// </summary>
    private static string? JoinTags(List<string> tags)
    {
        var kept = tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToList();

        var joined = string.Join(",", kept);
        while (joined.Length > 500 && kept.Count > 0)
        {
            kept.RemoveAt(kept.Count - 1);
            joined = string.Join(",", kept);
        }

        return joined.Length == 0 ? null : joined;
    }

    private async Task<UserProfileDto> LoadProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        if (user.Profile == null)
            throw new InvalidOperationException("User profile must be completed before generating a meal plan.");

        return UserProfileMapper.ToDto(user.Profile);
    }

    /// <summary>
    /// Loads what was planned for the period immediately before this one, so an instruction
    /// like "reuse last week's breakfasts" has a real week to work from.
    ///
    /// Only when guidance was given. Without an instruction to act on it, last week's meals
    /// are just a large block of text that quietly nudges every plan towards repeating
    /// itself - the opposite of what someone asking for a fresh week wants.
    /// </summary>
    private async Task AttachPreviousPeriodAsync(GenerateMealPlanRequest request)
    {
        request.PreviousPeriod = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Guidance))
        {
            return;
        }

        var length = request.EndDate.DayNumber - request.StartDate.DayNumber + 1;
        if (length <= 0)
        {
            return;
        }

        var previousEnd = request.StartDate.AddDays(-1);
        var previousStart = previousEnd.AddDays(-(length - 1));

        var planned = await _plannedMeals.ReadAsync(request.UserId, previousStart, previousEnd);
        var logged = await _plannedMeals.ReadLoggedAsync(request.UserId, previousStart, previousEnd);

        // What was actually eaten is the more honest answer to "what happened last period" -
        // a plan slot nothing was logged against falls back to what was merely planned for
        // it, but a logged slot always wins, since that is what really happened.
        var loggedSlots = logged.Select(meal => (meal.Date, meal.MealType)).ToHashSet();
        var merged = logged.Concat(planned.Where(meal => !loggedSlots.Contains((meal.Date, meal.MealType))));

        request.PreviousPeriod = merged
            .OrderBy(meal => meal.Date)
            .ThenBy(meal => meal.MealType)
            .Select(meal => $"{meal.Date:yyyy-MM-dd} {meal.MealType}: {meal.Label}")
            .ToList();
    }

    /// <summary>
    /// Folds the profile's standing exclusions into this request's. Every provider prompt
    /// already prints ExcludeIngredients, so doing it here reaches all of them - and a food
    /// someone is allergic to must not depend on remembering to type it each time.
    /// </summary>
    private static GenerateMealPlanRequest WithProfileExclusions(
        GenerateMealPlanRequest request,
        UserProfileDto profile)
    {
        if (profile.ExcludedFoods.Count == 0)
        {
            return request;
        }

        return new GenerateMealPlanRequest
        {
            UserId = request.UserId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PreferenceTags = request.PreferenceTags,
            // Copied across deliberately: this rebuild is the only path a request takes when
            // the profile excludes anything, so anything left out here is silently dropped
            // for exactly those users - which is how guidance would go missing.
            Guidance = request.Guidance,
            PreviousPeriod = request.PreviousPeriod,
            ExcludeIngredients = request.ExcludeIngredients
                .Concat(profile.ExcludedFoods)
                .DistinctBy(food => food.Trim().ToLowerInvariant())
                .ToList()
        };
    }

    private async Task<MealPlanDto> PersistAsync(GenerateMealPlanRequest request, GeneratedMealPlanDto generated)
    {
        var plan = new MealPlan
        {
            UserId = request.UserId,
            Name = $"AI Meal Plan {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsAIGenerated = true
        };

        // One recipe per distinct dish across the whole week, reusing the rows this person
        // already has: a plan that repeats the same breakfast five times should leave one
        // recipe behind, not five identical ones, and regenerating a week should leave none.
        var recipes = await LoadReusableRecipesAsync(request.UserId, generated);

        // Persist generated day entries
        foreach (var day in generated.Days)
        {
            foreach (var meal in day.Meals)
            {
                // Notes still carries the name: it is what the calendar card renders for an
                // entry with nothing else to show, and taking only RecipeDescription - which
                // most providers never even parse - left every generated plan showing seven
                // blank slots.
                var label = string.IsNullOrWhiteSpace(meal.RecipeName)
                    ? meal.RecipeDescription
                    : string.IsNullOrWhiteSpace(meal.RecipeDescription)
                        ? meal.RecipeName
                        : $"{meal.RecipeName} - {meal.RecipeDescription}";

                // The model writes a whole recipe for every meal - method, ingredients,
                // timings, macros - and all of it used to be dropped on the floor in favour
                // of that one label. Keeping it as a real recipe is what lets someone open a
                // planned meal and find out how to actually cook it.
                var recipe = await ResolveRecipeAsync(request.UserId, meal, recipes);

                plan.Entries.Add(new MealPlanEntry
                {
                    MealPlanId = plan.Id,
                    EntryDate = day.Date,
                    MealType = meal.MealType,
                    RecipeId = recipe?.Id,
                    // Attached so the response carries the meal's name straight away, rather
                    // than the client having to refetch the plan to learn what was saved.
                    Recipe = recipe,
                    Notes = label,
                    Servings = meal.Servings > 0 ? meal.Servings : 1f
                });
            }
        }

        await _unitOfWork.MealPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return GetMealPlanUseCase.MapToDto(plan);
    }

    /// <summary>
    /// The recipe row behind one generated meal, reusing one this person already has under
    /// the same name. Regenerating a week is routine, and without this every regeneration
    /// would leave another copy of every dish in the library.
    ///
    /// Deliberately not bookmarked, unlike a recipe kept from the assistant on purpose:
    /// filing twenty-one recipes into someone's saved list because they generated a week is
    /// not something they asked for. The planned meal itself is how they reach these.
    /// </summary>
    private async Task<Recipe?> ResolveRecipeAsync(
        Guid userId,
        GeneratedMealEntryDto meal,
        Dictionary<string, Recipe> known)
    {
        var name = meal.RecipeName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            // Nothing to name a recipe after - the entry keeps whatever label it has.
            return null;
        }

        if (known.TryGetValue(name, out var already))
        {
            return already;
        }

        var recipe = new Recipe
        {
            Name = name,
            Description = meal.RecipeDescription,
            Instructions = meal.Instructions,
            PrepTimeMinutes = meal.PrepTimeMinutes,
            CookTimeMinutes = meal.CookTimeMinutes,
            Servings = meal.Servings > 0 ? meal.Servings : 1,
            Tags = JoinTags(meal.Tags),
            ImageUrl = AiRecipeImage.Marker,
            IsAIGenerated = true,
            CreatedByUserId = userId,
            SourceName = "Foodeez AI",
            // The model is asked for per-meal figures and gives no fibre, sugar or sodium,
            // so those stay zero rather than being invented here.
            NutritionalInfoPerServing = new NutritionalInfo(
                meal.EstimatedCalories,
                meal.EstimatedProteinG,
                meal.EstimatedCarbsG,
                meal.EstimatedFatG,
                0f,
                0f,
                0f)
        };

        foreach (var ingredient in meal.Ingredients)
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
        known[name] = recipe;
        return recipe;
    }

    /// <summary>
    /// The recipes this person already has under the names the model just used, fetched in
    /// one query before the plan is walked.
    ///
    /// This used to be a SearchAsync per meal inside the loop - twenty-one queries for a
    /// seven-day plan, each a leading-wildcard LIKE over name and description that no index
    /// can serve, each pulling back every loose match with its ingredients and food items,
    /// only to keep the one row whose name matched exactly.
    /// </summary>
    private async Task<Dictionary<string, Recipe>> LoadReusableRecipesAsync(
        Guid userId, GeneratedMealPlanDto generated)
    {
        var names = generated.Days
            .SelectMany(day => day.Meals)
            .Select(meal => meal.RecipeName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await _unitOfWork.Recipes.GetOwnedByNamesAsync(userId, names);

        // Names compare case-insensitively here, so a plan asking for "Greek Salad" reuses a
        // stored "greek salad" rather than adding a second row. Two rows differing only in
        // case would both match; the first is as good as the other.
        var reusable = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in existing)
        {
            reusable.TryAdd(recipe.Name, recipe);
        }

        return reusable;
    }
}
