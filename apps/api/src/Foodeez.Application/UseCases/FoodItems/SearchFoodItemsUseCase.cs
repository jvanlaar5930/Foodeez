using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.FoodItems;

public class SearchFoodItemsUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFoodDataService _foodData;

    private const int DbSufficientThreshold = 3;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    public SearchFoodItemsUseCase(IUnitOfWork unitOfWork, IFoodDataService foodData)
    {
        _unitOfWork = unitOfWork;
        _foodData = foodData;
    }

    public async Task<List<FoodItemDto>> ExecuteAsync(string query, CancellationToken ct = default)
    {
        var dbResults = await _unitOfWork.FoodItems.SearchAsync(query);

        if (dbResults.Count >= DbSufficientThreshold)
            return dbResults.Select(FoodItemMapper.ToDto).ToList();

        var fdcResults = await _foodData.SearchFoodsAsync(query, 20, ct);
        if (fdcResults.Count == 0)
            return dbResults.Select(FoodItemMapper.ToDto).ToList();

        var fdcIds = fdcResults.Select(r => r.FdcId).ToList();
        var existingByFdc = await _unitOfWork.FoodItems.GetByFdcIdsAsync(fdcIds);
        var existingIdMap = existingByFdc.ToDictionary(f => f.FdcId!.Value);

        var now = DateTime.UtcNow;

        foreach (var fr in fdcResults)
        {
            if (existingIdMap.TryGetValue(fr.FdcId, out var existing))
            {
                if (existing.FdcSyncedAt.HasValue && now - existing.FdcSyncedAt.Value > StaleThreshold)
                    ApplyFdcUpdate(existing, fr, now);
                continue;
            }

            await _unitOfWork.FoodItems.AddAsync(MapToEntity(fr, now));
        }

        await _unitOfWork.SaveChangesAsync(ct);

        var refreshed = await _unitOfWork.FoodItems.SearchAsync(query);
        return refreshed.Select(FoodItemMapper.ToDto).ToList();
    }

    private static FoodItem MapToEntity(FdcFoodResult fr, DateTime syncedAt) => new FoodItem
    {
        Name = Capitalize(fr.Description),
        Brand = fr.BrandOwner ?? fr.BrandName,
        ServingSize = fr.ServingSize > 0 ? fr.ServingSize : 100f,
        ServingUnit = NormalizeUnit(fr.ServingSizeUnit),
        Category = fr.FoodCategory,
        Barcode = fr.GtinUpc,
        IsCustom = false,
        FdcId = fr.FdcId,
        FdcSyncedAt = syncedAt,
        NutritionalInfo = ExtractNutrition(fr),
    };

    private static void ApplyFdcUpdate(FoodItem existing, FdcFoodResult fr, DateTime now)
    {
        existing.Name = Capitalize(fr.Description);
        existing.Brand = fr.BrandOwner ?? fr.BrandName;
        existing.Category = fr.FoodCategory;
        existing.NutritionalInfo = ExtractNutrition(fr);
        existing.FdcSyncedAt = now;
    }

    private static NutritionalInfo ExtractNutrition(FdcFoodResult fr)
    {
        float Get(params string[] names)
        {
            foreach (var name in names)
            {
                var n = fr.FoodNutrients.FirstOrDefault(
                    x => x.NutrientName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (n != null) return (float)n.Value;
            }
            return 0f;
        }

        return new NutritionalInfo(
            calories:      Get("Energy", "Calories"),
            protein:       Get("Protein"),
            carbohydrates: Get("Carbohydrate, by difference", "Carbohydrates"),
            fat:           Get("Total lipid (fat)", "Fat", "Total Fat"),
            fiber:         Get("Fiber, total dietary", "Dietary Fiber", "Fiber"),
            sugar:         Get("Sugars, total including NLEA", "Total Sugars", "Sugar"),
            sodium:        Get("Sodium, Na", "Sodium")
        );
    }


    private static string Capitalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
    }

    private static string NormalizeUnit(string unit) =>
        string.IsNullOrWhiteSpace(unit) ? "g" : unit.ToLowerInvariant() switch
        {
            "g" or "gram" or "grams" => "g",
            "ml" or "milliliter" or "milliliters" => "ml",
            "oz" or "ounce" or "ounces" => "oz",
            _ => unit
        };
}
