using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Grocery;
using Foodeez.Domain.Entities;

namespace Foodeez.Application.UseCases.Grocery;

/// <summary>
/// The edits a shopping list actually gets: ticking things off, swapping an item for what
/// the shop had, and adding what the plan never knew about.
///
/// These are ordinary writes rather than AI calls - the list is the reader's from the moment
/// it is compiled, and nothing here goes near the model.
/// </summary>
public class EditGroceryListUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public EditGroceryListUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GroceryItemDto?> AddAsync(
        Guid userId, Guid listId, GroceryItemRequest request, CancellationToken ct = default)
    {
        var list = await LoadAsync(userId, listId);
        if (list == null)
        {
            return null;
        }

        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return null;
        }

        var item = new GroceryListItem
        {
            GroceryListId = list.Id,
            Name = name,
            Quantity = request.Quantity.Trim(),
            Category = GroceryListPrompt.NormalizeCategory(request.Category),
            IsChecked = request.IsChecked,
            // Marked as the shopper's own, which is what keeps it through a rebuild.
            IsCustom = true,
            SortOrder = -1
        };

        list.Items.Add(item);
        await _unitOfWork.GroceryLists.AddItemAsync(item);
        await _unitOfWork.SaveChangesAsync(ct);

        return GroceryListMapper.ToDto(item);
    }

    /// <summary>
    /// Changes one line. This is the swap: renaming an item and adjusting its amount is how
    /// "chicken thighs instead of breast" reaches the list.
    /// </summary>
    public async Task<GroceryItemDto?> UpdateAsync(
        Guid userId, Guid listId, Guid itemId, GroceryItemRequest request, CancellationToken ct = default)
    {
        var list = await LoadAsync(userId, listId);
        var item = list?.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return null;
        }

        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            return null;
        }

        item.Name = name;
        item.Quantity = request.Quantity.Trim();
        item.Category = GroceryListPrompt.NormalizeCategory(request.Category);
        item.IsChecked = request.IsChecked;

        // An edited line is the shopper's, whatever it started as, so a rebuild leaves it be.
        item.IsCustom = true;

        await _unitOfWork.SaveChangesAsync(ct);
        return GroceryListMapper.ToDto(item);
    }

    /// <summary>Ticking off, on its own, so a checkbox does not have to send the whole line.</summary>
    public async Task<GroceryItemDto?> SetCheckedAsync(
        Guid userId, Guid listId, Guid itemId, bool isChecked, CancellationToken ct = default)
    {
        var list = await LoadAsync(userId, listId);
        var item = list?.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
        {
            return null;
        }

        item.IsChecked = isChecked;
        await _unitOfWork.SaveChangesAsync(ct);
        return GroceryListMapper.ToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid listId, Guid itemId, CancellationToken ct = default)
    {
        var list = await LoadAsync(userId, listId);
        var item = list?.Items.FirstOrDefault(i => i.Id == itemId);
        if (list == null || item == null)
        {
            return false;
        }

        list.Items.Remove(item);
        _unitOfWork.GroceryLists.RemoveItem(item);
        await _unitOfWork.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Someone else's list reads as missing rather than forbidden, for the same reason as
    /// the meal plan endpoints: confirming it exists tells an unauthorised caller too much.
    /// </summary>
    private async Task<GroceryList?> LoadAsync(Guid userId, Guid listId)
    {
        var list = await _unitOfWork.GroceryLists.GetWithItemsAsync(listId);
        return list == null || list.UserId != userId ? null : list;
    }
}
