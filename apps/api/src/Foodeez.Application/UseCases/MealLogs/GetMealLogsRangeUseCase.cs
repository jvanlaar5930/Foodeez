using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.UseCases.MealLogs;

public class GetMealLogsRangeUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMealLogsRangeUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MealLogDto>> ExecuteAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateRangeAsync(userId, startDate, endDate);
        return logs.Select(MealLogMapper.ToDto).ToList();
    }
}
