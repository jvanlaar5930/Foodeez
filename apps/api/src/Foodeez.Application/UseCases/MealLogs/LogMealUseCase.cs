using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.UseCases.MealLogs;

public class LogMealUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public LogMealUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MealLogDto> ExecuteAsync(LogMealRequest request)
    {
        var mealLog = new Domain.Entities.MealLog();

        await MealLogRequestApplier.ApplyAsync(mealLog, request, _unitOfWork);

        await _unitOfWork.MealLogs.AddAsync(mealLog);
        await _unitOfWork.SaveChangesAsync();

        return MealLogMapper.ToDto(mealLog);
    }
}
