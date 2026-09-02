using Foodeez.Application.DTOs.AI;

namespace Foodeez.Application.Common;

/// <summary>
/// Fills a meal analysis request with the user's standing food exclusions.
///
/// Read from the profile rather than taken from the caller: a browser that forgot to send
/// them would otherwise get suggestions built on someone's allergy.
/// </summary>
public static class MealExclusions
{
    public static async Task ApplyAsync(MealAnalysisRequest request, IUnitOfWork unitOfWork)
    {
        if (request.UserId is not { } userId)
        {
            request.ExcludedFoods = new List<string>();
            return;
        }

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        request.ExcludedFoods = user?.Profile?.ExcludedFoods.ToList() ?? new List<string>();
    }
}
