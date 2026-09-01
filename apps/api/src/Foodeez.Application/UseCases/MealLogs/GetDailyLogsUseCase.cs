using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.UseCases.MealLogs;

public class GetDailyLogsUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public GetDailyLogsUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MealLogDto>> ExecuteAsync(Guid userId, DateOnly date)
    {
        var logs = await _unitOfWork.MealLogs.GetByUserAndDateAsync(userId, date);
        return logs.Select(MealLogMapper.ToDto).ToList();
    }
}
