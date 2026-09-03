using System.Runtime.CompilerServices;
using System.Text;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.UseCases.Grocery;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

public class GenerateAIMealPlanUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;
    private readonly IStreamingAIService _streaming;
    private readonly PlannedMealReader _plannedMeals;

    public GenerateAIMealPlanUseCase(
        IUnitOfWork unitOfWork,
        IAIService aiService,
        IStreamingAIService streaming,
        PlannedMealReader plannedMeals)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _streaming = streaming;
        _plannedMeals = plannedMeals;
    }

    public async Task<MealPlanDto> ExecuteAsync(GenerateMealPlanRequest request, CancellationToken ct = default)
    {
        var profileDto = await LoadProfileAsync(request.UserId);
        await AttachPreviousPeriodAsync(request);

        var generated = await _aiService.GenerateMealPlanAsync(
            WithProfileExclusions(request, profileDto), profileDto, ct);

        // Providers swallow their own transport errors and hand back an empty result, so an
        // outage arrives here looking exactly like a plan with no days in it. Saving that
        // would hand the user a persisted, permanently blank week and report success -
        // refusing is the only honest answer, and it leaves nothing to clean up.
        if (generated.Days.Count == 0)
            throw new AIGenerationFailedException(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");

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
            _streaming, MealPlanPrompt.Build(WithProfileExclusions(request, profileDto), profileDto), transcript, ct))
        {
            yield return AIStreamEvent.Delta(delta);
        }

        var generated = MealPlanPrompt.Parse(transcript.ToString());
        if (generated.Days.Count == 0)
        {
            // Same judgement as the blocking path: a plan with no days is an outage, not a
            // plan, and saving it would hand the user a permanently blank week.
            yield return AIStreamEvent.Error(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");
            yield break;
        }

        yield return AIStreamEvent.Result(await PersistAsync(request, generated));
    }

    private async Task<UserProfileDto> LoadProfileAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{userId}' was not found.");

        if (user.Profile == null)
            throw new InvalidOperationException("User profile must be completed before generating a meal plan.");

        return new UserProfileDto
        {
            UserId = user.Profile.UserId,
            HeightCm = user.Profile.HeightCm,
            WeightKg = user.Profile.WeightKg,
            TargetWeightKg = user.Profile.TargetWeightKg,
            Age = user.Profile.Age,
            Gender = user.Profile.Gender,
            ActivityLevel = user.Profile.ActivityLevel,
            DietaryGoal = user.Profile.DietaryGoal,
            DailyCalorieTarget = user.Profile.DailyCalorieTarget,
            DailyProteinTargetG = user.Profile.DailyProteinTargetG,
            DailyCarbTargetG = user.Profile.DailyCarbTargetG,
            DailyFatTargetG = user.Profile.DailyFatTargetG,
            Notes = user.Profile.Notes,
            ExcludedFoods = user.Profile.ExcludedFoods.ToList(),
            ProfileCompleted = user.Profile.ProfileCompleted
        };
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

        // Persist generated day entries
        foreach (var day in generated.Days)
        {
            foreach (var meal in day.Meals)
            {
                // The name is the only part of a generated meal a reader actually sees:
                // these entries have no Recipe row behind them, so RecipeName on the DTO is
                // always null and Notes is the one field that reaches the card. Taking only
                // RecipeDescription - which most providers never even parse - left every
                // generated plan showing seven blank slots.
                var label = string.IsNullOrWhiteSpace(meal.RecipeName)
                    ? meal.RecipeDescription
                    : string.IsNullOrWhiteSpace(meal.RecipeDescription)
                        ? meal.RecipeName
                        : $"{meal.RecipeName} - {meal.RecipeDescription}";

                plan.Entries.Add(new MealPlanEntry
                {
                    MealPlanId = plan.Id,
                    EntryDate = day.Date,
                    MealType = meal.MealType,
                    Notes = label,
                    Servings = meal.Servings > 0 ? meal.Servings : 1f
                });
            }
        }

        await _unitOfWork.MealPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return GetMealPlanUseCase.MapToDto(plan);
    }
}
