using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.FoodItems;

/// <summary>
/// Records a food somebody entered by hand.
///
/// This was written out in the controller, which meant an HTTP endpoint knew how to build a
/// FoodItem and a NutritionalInfo - so the next caller that needed to do the same thing would
/// have had to either copy it or go through HTTP.
/// </summary>
public class CreateFoodItemUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateFoodItemUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <param name="ownerId">
    /// Taken from the token by the caller, never from the request body: a client should not be
    /// able to file its custom foods under somebody else's account.
    /// </param>
    public async Task<FoodItemDto> ExecuteAsync(CreateFoodItemRequest request, Guid ownerId)
    {
        var foodItem = new FoodItem
        {
            Name = request.Name,
            Brand = request.Brand,
            ServingSize = request.ServingSize,
            ServingUnit = request.ServingUnit,
            Category = request.Category,
            Barcode = request.Barcode,
            IsCustom = true,
            CreatedByUserId = ownerId,
            NutritionalInfo = new NutritionalInfo(
                request.Calories,
                request.Protein,
                request.Carbohydrates,
                request.Fat,
                request.Fiber,
                request.Sugar,
                request.Sodium)
        };

        await _unitOfWork.FoodItems.AddAsync(foodItem);
        await _unitOfWork.SaveChangesAsync();

        return FoodItemMapper.ToDto(foodItem);
    }
}
