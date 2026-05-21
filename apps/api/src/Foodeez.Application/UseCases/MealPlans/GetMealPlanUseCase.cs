using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

public class GetMealPlanUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMealPlanUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MealPlanDto>> ExecuteAsync(Guid userId)
    {
        var plans = await _unitOfWork.MealPlans.GetByUserIdAsync(userId);
        return plans.Select(MapToDto).ToList();
    }

    internal static MealPlanDto MapToDto(MealPlan plan)
    {
        var entriesByDate = plan.Entries
            .GroupBy(e => e.EntryDate.ToString("yyyy-MM-dd"))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => new MealPlanEntryDto
                {
                    Id = e.Id,
                    MealPlanId = e.MealPlanId,
                    EntryDate = e.EntryDate,
                    MealType = e.MealType,
                    RecipeId = e.RecipeId,
                    RecipeName = e.Recipe?.Name,
                    FoodItemId = e.FoodItemId,
                    FoodItemName = e.FoodItem?.Name,
                    Notes = e.Notes,
                    Servings = e.Servings
                }).ToList()
            );

        return new MealPlanDto
        {
            Id = plan.Id,
            UserId = plan.UserId,
            Name = plan.Name,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            IsAIGenerated = plan.IsAIGenerated,
            EntriesByDate = entriesByDate
        };
    }
}
