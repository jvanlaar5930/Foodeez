using Foodeez.Application.Common;

namespace Foodeez.Application.UseCases.MealLogs;

public class DeleteMealLogUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMealLogUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid mealLogId)
    {
        var mealLog = await _unitOfWork.MealLogs.GetDetailedByIdAsync(mealLogId);
        if (mealLog == null)
        {
            throw new KeyNotFoundException($"MealLog with id '{mealLogId}' was not found.");
        }

        _unitOfWork.MealLogs.Delete(mealLog);
        await _unitOfWork.SaveChangesAsync();
    }
}
