using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.MealPlans;

public class CreateMealPlanUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateMealPlanUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealPlanDto> ExecuteAsync(CreateMealPlanRequest request)
    {
        var plan = new MealPlan
        {
            UserId = request.UserId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsAIGenerated = false
        };

        await _unitOfWork.MealPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        return GetMealPlanUseCase.MapToDto(plan);
    }
}
