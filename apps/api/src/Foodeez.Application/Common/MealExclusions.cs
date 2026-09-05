using Foodeez.Application.DTOs.AI;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.Common;

/// <summary>
/// A user's standing food exclusions - allergies, intolerances, dislikes.
///
/// Always read from the profile, never taken from the caller: a browser that forgot to send
/// them would otherwise get suggestions built on someone's allergy. Every AI feature needs
/// them and each one used to load them itself, in five near-identical private helpers.
/// </summary>
public static class MealExclusions
{
    /// <summary>
    /// The exclusions on a profile that may not exist yet. An absent profile means no known
    /// exclusions, not "unknown" - the prompt simply says nothing about them.
    /// </summary>
    public static List<string> Of(UserProfile? profile) =>
        profile?.ExcludedFoods.ToList() ?? new List<string>();

    /// <summary>The exclusions of a user identified only by id.</summary>
    public static async Task<List<string>> LoadAsync(IUnitOfWork unitOfWork, Guid userId) =>
        Of((await unitOfWork.Users.GetByIdAsync(userId))?.Profile);

    /// <summary>Fills a meal analysis request with the owner's exclusions.</summary>
    public static async Task ApplyAsync(MealAnalysisRequest request, IUnitOfWork unitOfWork)
    {
        request.ExcludedFoods = request.UserId is { } userId
            ? await LoadAsync(unitOfWork, userId)
            : new List<string>();
    }
}
