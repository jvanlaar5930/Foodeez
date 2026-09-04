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
        // This replaces every item on the meal (clear, then re-add), so it is safe to redo
        // from scratch. That matters because the database provider retries a save on a
        // transient connection blip - if the first attempt's delete actually committed just
        // before the connection dropped, the retry re-issues the same delete against a row
        // that is already gone and EF reports it as a concurrency conflict, even though
        // nothing is actually wrong. One reload-and-reapply clears that up; a second failure
        // is a real conflict and is left to surface.
        for (var attempt = 0; ; attempt++)
        {
            var mealLog = await _unitOfWork.MealLogs.GetDetailedByIdAsync(mealLogId);
            if (mealLog == null)
            {
                throw new KeyNotFoundException($"MealLog with id '{mealLogId}' was not found.");
            }

            // No Update() call here: the meal was loaded by a tracking query, so the change
            // tracker already picks up the edits, the removed items and the new ones. Calling
            // Update() would instead force every freshly built item to Modified - their Ids are
            // assigned in the constructor, so EF cannot tell them from existing rows - and the
            // resulting UPDATE against rows that do not exist yet fails as a phantom conflict.
            await MealLogRequestApplier.ApplyAsync(mealLog, request, _unitOfWork);

            try
            {
                await _unitOfWork.SaveChangesAsync();
                return MealLogMapper.ToDto(mealLog);
            }
            catch (ConcurrencyConflictException) when (attempt == 0)
            {
            }
        }
    }
}
