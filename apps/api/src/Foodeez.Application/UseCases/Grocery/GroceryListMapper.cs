using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Grocery;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Grocery;

internal static class GroceryListMapper
{
    public static GroceryListDto ToDto(GroceryList list, int plannedMealCount, bool isStale) => new()
    {
        Id = list.Id,
        StartDate = list.StartDate,
        EndDate = list.EndDate,
        GeneratedAt = list.GeneratedAt,
        IsStale = isStale,
        PlannedMealCount = plannedMealCount,
        Items = list.Items
            // Aisle order, so the list is walked rather than hunted through. Sort order
            // within an aisle is the compiled order; hand-added items keep where they landed.
            .OrderBy(i => GroceryListPrompt.CategoryRank(i.Category))
            .ThenBy(i => i.SortOrder)
            .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(ToDto)
            .ToList()
    };

    public static GroceryItemDto ToDto(GroceryListItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Quantity = item.Quantity,
        Category = item.Category,
        Source = item.Source,
        IsChecked = item.IsChecked,
        IsCustom = item.IsCustom,
        SortOrder = item.SortOrder
    };
}
