using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.UseCases.MealLogs;

public class UpdateMealLogUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMealLogUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealLogDto> ExecuteAsync(Guid mealLogId, LogMealRequest request)
    {
        var mealLog = await _unitOfWork.MealLogs.GetDetailedByIdAsync(mealLogId);
        if (mealLog == null)
        {
            throw new KeyNotFoundException($"MealLog with id '{mealLogId}' was not found.");
        }

        await MealLogRequestApplier.ApplyAsync(mealLog, request, _unitOfWork);

        _unitOfWork.MealLogs.Update(mealLog);
        await _unitOfWork.SaveChangesAsync();

        return MealLogMapper.ToDto(mealLog);
    }
}
