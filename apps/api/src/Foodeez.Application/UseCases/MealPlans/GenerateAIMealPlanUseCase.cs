using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.UseCases.Grocery;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
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

    /// <summary>
    /// How many days in a row may fail before the run is abandoned.
    ///
    /// One failed day is a day the model made a mess of; two in a row is the provider being
    /// down, and there is nothing to gain from spending several more minutes discovering that
    /// five more times. Whatever was written before the run stopped is kept and reported.
    /// </summary>
    private const int MaxConsecutiveFailures = 2;

    public async Task<MealPlanDto> ExecuteAsync(GenerateMealPlanRequest request, CancellationToken ct = default)
    {
        MealPlanGenerationResultDto? outcome = null;

        // The same day-by-day walk the streamed path takes, with the narration dropped on the
        // floor. Two implementations of this would be two sets of rules about what happens to
        // a half-written week.
        await foreach (var streamEvent in ExecuteStreamAsync(request, ct))
        {
            if (streamEvent.Type == "result" && streamEvent.Data is MealPlanGenerationResultDto result)
            {
                outcome = result;
            }
            else if (streamEvent.Type == "error")
            {
                throw new AIGenerationFailedException(
                    streamEvent.Message ?? "The AI service could not produce a meal plan right now.");
            }
        }

        if (outcome?.Plan == null)
        {
            throw new AIGenerationFailedException(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");
        }

        return outcome.Plan;
    }

    /// <summary>
    /// Generates the plan a day at a time, saving each day before asking for the next.
    ///
    /// The whole week used to be one call: one prompt, one answer, one parse, one save at the
    /// end. Anything that went wrong anywhere in it cost the entire week and said only that
    /// "the AI service could not produce a meal plan" - no indication of where it had got to,
    /// and nothing kept. Now each day stands on its own, so a failure costs that day, the
    /// caller is told which date it was, and the days that worked are already saved.
    /// </summary>
    public async IAsyncEnumerable<AIStreamEvent> ExecuteStreamAsync(
        GenerateMealPlanRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var profileDto = await LoadProfileAsync(request.UserId);
        await AttachPreviousPeriodAsync(request);

        var effective = WithProfileExclusions(request, profileDto);
        var dates = DatesOf(request);

        if (dates.Count == 0)
        {
            yield return AIStreamEvent.Error("That date range covers no days. Pick an end date on or after the start date.");
            yield break;
        }

        // What is already in the calendar across the range, so no generated meal can displace
        // something the user put there by hand.
        var occupied = await ReadOccupiedSlotsAsync(request.UserId, request.StartDate, request.EndDate);

        MealPlan? plan = null;
        var recipes = new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase);
        var planned = new List<string>();
        var failed = new List<string>();
        var kept = new List<string>();
        var consecutiveFailures = 0;
        var stoppedEarly = false;

        for (var index = 0; index < dates.Count; index++)
        {
            var date = dates[index];
            var slotsTaken = occupied.TryGetValue(date, out var taken) ? taken : new List<string>();

            yield return AIStreamEvent.Progress(new MealPlanProgressDto
            {
                Date = date,
                DayNumber = index + 1,
                TotalDays = dates.Count,
                Status = "planning"
            });

            var prompt = MealPlanPrompt.BuildDay(effective, profileDto, date, slotsTaken, planned);

            var transcript = new StringBuilder();
            var failure = null as string;

            // The stream is stepped by hand because a provider fault has to become this day's
            // failure rather than the whole run's: yield return cannot appear inside a try
            // with a catch, so the reading and the yielding are separated.
            await foreach (var delta in ReadDayAsync(prompt, transcript, ct))
            {
                if (delta.Failure is { } message)
                {
                    failure = message;
                    break;
                }

                yield return AIStreamEvent.Delta(delta.Text!);
            }

            var day = failure == null
                ? MealPlanPrompt.ParseDay(transcript.ToString(), date)
                : new GeneratedDayDto { Date = date };

            if (failure != null || day.Meals.Count == 0)
            {
                if (failure == null)
                {
                    _logger.LogWarning(
                        "AI meal plan produced no usable meals for {Date}. Raw response ({Length} chars): {Response}",
                        date, transcript.Length, Truncate(transcript.ToString(), 2000));
                }

                failed.Add(date.ToString("yyyy-MM-dd"));
                consecutiveFailures++;

                yield return AIStreamEvent.Progress(new MealPlanProgressDto
                {
                    Date = date,
                    DayNumber = index + 1,
                    TotalDays = dates.Count,
                    Status = "failed",
                    Message = failure ?? "Nothing usable came back for this day."
                });

                if (consecutiveFailures >= MaxConsecutiveFailures)
                {
                    stoppedEarly = true;
                    break;
                }

                continue;
            }

            consecutiveFailures = 0;

            // Created on the first day that actually produced something, not before: a run
            // that fails on day one leaves no empty plan behind to clean up.
            plan ??= await StartPlanAsync(request);

            var saved = await SaveDayAsync(plan, request.UserId, day, slotsTaken, recipes);

            foreach (var meal in day.Meals)
            {
                var name = meal.RecipeName?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    planned.Add($"{date:yyyy-MM-dd} {meal.MealType}: {name}");
                }
            }

            if (saved.Count == 0)
            {
                kept.Add(date.ToString("yyyy-MM-dd"));
            }
            else
            {
                yield return AIStreamEvent.Part(new MealPlanDayDto
                {
                    Date = date,
                    PlanId = plan.Id,
                    Entries = saved.Select(GetMealPlanUseCase.MapEntry).ToList()
                });
            }

            yield return AIStreamEvent.Progress(new MealPlanProgressDto
            {
                Date = date,
                DayNumber = index + 1,
                TotalDays = dates.Count,
                Status = saved.Count == 0 ? "kept" : "saved"
            });
        }

        if (plan == null)
        {
            // Not one day survived, so there is nothing to show and nothing was stored.
            yield return AIStreamEvent.Error(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");
            yield break;
        }

        yield return AIStreamEvent.Result(new MealPlanGenerationResultDto
        {
            Plan = GetMealPlanUseCase.MapToDto(plan),
            FailedDates = failed,
            KeptDates = kept,
            StoppedEarly = stoppedEarly,
            Message = Summarise(dates.Count, failed, kept, stoppedEarly)
        });
    }

    /// <summary>A delta from the model, or the reason there will not be any more.</summary>
    private readonly record struct DayChunk(string? Text, string? Failure);

    /// <summary>
    /// One day's narration, with a provider fault turned into a value rather than an exception
    /// so the caller can keep going to the next day.
    /// </summary>
    private async IAsyncEnumerable<DayChunk> ReadDayAsync(
        string prompt,
        StringBuilder transcript,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var stream = AINarration.NarrateAsync(_streaming, prompt, transcript, ct).GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                // Filled in inside the try and acted on outside it: C# will not allow a yield
                // anywhere in a try that has a catch.
                string? text = null;
                string? failure = null;
                var finished = false;

                try
                {
                    if (await stream.MoveNextAsync())
                    {
                        text = stream.Current;
                    }
                    else
                    {
                        finished = true;
                    }
                }
                catch (OperationCanceledException)
                {
                    // The caller gave up on the whole run; that is not this day's problem.
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AI meal plan: the provider failed while writing a day.");
                    failure = "The AI service stopped part-way through this day.";
                }

                if (finished)
                {
                    yield break;
                }

                if (failure != null)
                {
                    yield return new DayChunk(null, failure);
                    yield break;
                }

                yield return new DayChunk(text, null);
            }
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    private static List<DateOnly> DatesOf(GenerateMealPlanRequest request)
    {
        var dates = new List<DateOnly>();

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }

    /// <summary>A sentence about how the run went, so no client has to compose one itself.</summary>
    private static string Summarise(int total, List<string> failed, List<string> kept, bool stoppedEarly)
    {
        var written = total - failed.Count - kept.Count;

        if (stoppedEarly)
        {
            return $"Stopped after {failed.Count} days in a row could not be planned - the AI service looks " +
                   $"to be unavailable. {written} of {total} days were saved; the rest are untouched.";
        }

        if (failed.Count == 0)
        {
            return kept.Count == 0
                ? $"All {total} days planned."
                : $"{written} days planned; {kept.Count} left as they were, already full.";
        }

        return $"{written} of {total} days planned. These could not be: {string.Join(", ", failed)}. " +
               "Everything else was saved - generate again to fill them in.";
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

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

    /// <summary>
    /// The plan row, saved on its own before any day is written into it.
    ///
    /// Saved immediately rather than at the end because every day that follows is committed
    /// against it: an unsaved parent would make each day's save the whole plan's save, and a
    /// failure on day four would take days one to three with it - the very thing this design
    /// exists to prevent.
    /// </summary>
    private async Task<MealPlan> StartPlanAsync(GenerateMealPlanRequest request)
    {
        var plan = new MealPlan
        {
            UserId = request.UserId,
            Name = $"AI Meal Plan {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsAIGenerated = true
        };

        await _unitOfWork.MealPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return plan;
    }

    /// <summary>
    /// Writes one day's meals and commits them, skipping any slot that is already spoken for.
    /// </summary>
    /// <returns>The entries actually added - empty when every slot that day was already full.</returns>
    private async Task<List<MealPlanEntry>> SaveDayAsync(
        MealPlan plan,
        Guid userId,
        GeneratedDayDto day,
        IReadOnlyCollection<string> occupied,
        Dictionary<string, Recipe> recipes)
    {
        await LoadReusableRecipesAsync(userId, day, recipes);

        var taken = occupied
            .Select(line => line.Split(':', 2)[0].Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var added = new List<MealPlanEntry>();
        var usedThisDay = new HashSet<MealType>();

        foreach (var meal in day.Meals)
        {
            // Told plainly in the prompt not to plan these, but a model that does it anyway
            // must not be allowed to overwrite something a person put there.
            if (taken.Contains(meal.MealType.ToString()) || !usedThisDay.Add(meal.MealType))
            {
                continue;
            }

            // Notes still carries the name: it is what the calendar card renders for an
            // entry with nothing else to show, and taking only RecipeDescription - which
            // most providers never even parse - left every generated plan showing seven
            // blank slots.
            var label = string.IsNullOrWhiteSpace(meal.RecipeName)
                ? meal.RecipeDescription
                : string.IsNullOrWhiteSpace(meal.RecipeDescription)
                    ? meal.RecipeName
                    : $"{meal.RecipeName} - {meal.RecipeDescription}";

            // The model writes a whole recipe for every meal - method, ingredients, timings,
            // macros - and all of it used to be dropped on the floor in favour of that one
            // label. Keeping it as a real recipe is what lets someone open a planned meal and
            // find out how to actually cook it.
            var recipe = await ResolveRecipeAsync(userId, meal, recipes);

            var entry = new MealPlanEntry
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
            };

            plan.Entries.Add(entry);
            await _unitOfWork.MealPlans.AddEntryAsync(entry);
            added.Add(entry);
        }

        if (added.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return added;
    }

    /// <summary>
    /// The slots already filled across the range, as "MealType: name" lines per date.
    ///
    /// Read once before the walk rather than per day: it is one pass over the user's plans
    /// either way, and doing it inside the loop would repeat that for every date.
    /// </summary>
    private async Task<Dictionary<DateOnly, List<string>>> ReadOccupiedSlotsAsync(
        Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var existing = await _plannedMeals.ReadAsync(userId, startDate, endDate);

        return existing
            .GroupBy(meal => meal.Date)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(meal => $"{meal.MealType}: {meal.Label}")
                    .ToList());
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
            Tags = RecipeTags.Join(meal.Tags),
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
    private async Task LoadReusableRecipesAsync(
        Guid userId, GeneratedDayDto day, Dictionary<string, Recipe> known)
    {
        var names = day.Meals
            .Select(meal => meal.RecipeName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Where(name => !known.ContainsKey(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return;
        }

        var existing = await _unitOfWork.Recipes.GetOwnedByNamesAsync(userId, names);

        // Names compare case-insensitively here, so a plan asking for "Greek Salad" reuses a
        // stored "greek salad" rather than adding a second row. Two rows differing only in
        // case would both match; the first is as good as the other.
        //
        // The dictionary is carried across the whole run rather than rebuilt per day, which is
        // what stops a week that eats the same breakfast five times leaving five identical
        // recipes behind.
        foreach (var recipe in existing)
        {
            known.TryAdd(recipe.Name, recipe);
        }
    }
}
