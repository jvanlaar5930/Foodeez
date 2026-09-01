using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

public class GenerateAIMealPlanUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;

    public GenerateAIMealPlanUseCase(IUnitOfWork unitOfWork, IAIService aiService)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
    }

    public async Task<MealPlanDto> ExecuteAsync(GenerateMealPlanRequest request, CancellationToken ct = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);
        if (user == null)
            throw new KeyNotFoundException($"User with id '{request.UserId}' was not found.");

        if (user.Profile == null)
            throw new InvalidOperationException("User profile must be completed before generating a meal plan.");

        var profileDto = new UserProfileDto
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
            ProfileCompleted = user.Profile.ProfileCompleted
        };

        var generated = await _aiService.GenerateMealPlanAsync(request, profileDto, ct);

        // Providers swallow their own transport errors and hand back an empty result, so an
        // outage arrives here looking exactly like a plan with no days in it. Saving that
        // would hand the user a persisted, permanently blank week and report success -
        // refusing is the only honest answer, and it leaves nothing to clean up.
        if (generated.Days.Count == 0)
            throw new AIGenerationFailedException(
                "The AI service could not produce a meal plan right now. Please try again in a moment.");

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

                var entry = new MealPlanEntry
                {
                    MealPlanId = plan.Id,
                    EntryDate = day.Date,
                    MealType = meal.MealType,
                    Notes = label,
                    Servings = meal.Servings > 0 ? meal.Servings : 1f
                };
                plan.Entries.Add(entry);
            }
        }

        await _unitOfWork.MealPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return GetMealPlanUseCase.MapToDto(plan);
    }
}
