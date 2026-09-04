using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.MealLogs;

internal static class MealLogRequestApplier
{
    public static async Task ApplyAsync(MealLog mealLog, LogMealRequest request, IUnitOfWork unitOfWork)
    {
        mealLog.UserId = request.UserId;
        mealLog.LogDate = request.LogDate;
        mealLog.MealType = request.MealType;
        mealLog.Notes = request.Notes;
        mealLog.Items.Clear();

        foreach (var itemRequest in request.Items)
        {
            var foodItem = await unitOfWork.FoodItems.GetByIdAsync(itemRequest.FoodItemId);
            if (foodItem == null)
            {
                throw new KeyNotFoundException(
                    $"FoodItem with id '{itemRequest.FoodItemId}' was not found.");
            }

            var quantity = itemRequest.Quantity;
            var scaleFactor = foodItem.ServingSize > 0
                ? quantity / foodItem.ServingSize
                : quantity;

            mealLog.Items.Add(new MealLogItem
            {
                MealLogId = mealLog.Id,
                FoodItemId = foodItem.Id,
                Quantity = quantity,
                Unit = itemRequest.Unit,
                NutritionalInfo = foodItem.NutritionalInfo.Scale(scaleFactor),
                FoodItem = foodItem
            });
        }

        await ApplyAnalysisAsync(mealLog, request, unitOfWork);
    }

    /// <summary>
    /// Keeps the stored analysis honest: an analysis sent with the request describes exactly
    /// the items just applied and is kept, and an older one survives only while the meal it
    /// was generated from is unchanged.
    /// </summary>
    private static async Task ApplyAnalysisAsync(MealLog mealLog, LogMealRequest request, IUnitOfWork unitOfWork)
    {
        var excludedFoods = await MealExclusions.LoadAsync(unitOfWork, request.UserId);
        var fingerprint = MealAnalysisFingerprint.For(mealLog, excludedFoods);

        if (request.Analysis is { } analysis)
        {
            mealLog.Analysis = new MealAnalysis(
                analysis.Score,
                analysis.Completeness,
                analysis.Missing,
                analysis.Suggestions,
                fingerprint,
                analysis.GeneratedAt ?? DateTime.UtcNow);
        }
        else if (mealLog.Analysis is { } existing && !existing.Matches(fingerprint))
        {
            mealLog.Analysis = null;
        }
    }
}
