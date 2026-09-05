using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Foodeez.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

public class UpdateMealLogUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMealLogRepository> _mealLogRepoMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly UpdateMealLogUseCase _sut;

    public UpdateMealLogUseCaseTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mealLogRepoMock = new Mock<IMealLogRepository>();
        _foodItemRepoMock = new Mock<IFoodItemRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(u => u.MealLogs).Returns(_mealLogRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepoMock.Object);

        // MealLogRequestApplier reads the user's excluded foods to fingerprint the analysis,
        // so the Users repository has to be there even for tests that are not about analysis.
        // A null user is a valid answer to that lookup and keeps these tests on their subject.
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _userRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new UpdateMealLogUseCase(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesMealItemsAndTotals()
    {
        var userId = Guid.NewGuid();
        var existingFood = CreateFoodItem("Oats", 100f, 380f, 13f, 67f, 7f);
        var replacementFood = CreateFoodItem("Greek Yogurt", 150f, 120f, 15f, 6f, 4f);
        var existingLog = CreateMealLog(userId, existingFood, 100f, MealType.Breakfast);
        var mealLogId = existingLog.Id;

        _mealLogRepoMock.Setup(r => r.GetDetailedByIdAsync(mealLogId)).ReturnsAsync(existingLog);
        _foodItemRepoMock.Setup(r => r.GetByIdAsync(replacementFood.Id)).ReturnsAsync(replacementFood);

        var request = new LogMealRequest
        {
            UserId = userId,
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Lunch,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = replacementFood.Id, Quantity = 300f, Unit = "g" }
            }
        };

        var result = await _sut.ExecuteAsync(mealLogId, request);

        result.MealType.Should().Be(MealType.Lunch);
        result.Items.Should().ContainSingle();
        result.Items[0].FoodItem.Name.Should().Be("Greek Yogurt");
        result.Items[0].Quantity.Should().Be(300f);
        result.TotalNutrition.Calories.Should().BeApproximately(240f, 0.1f);

        // The meal comes back from a tracking query, so the edits save themselves. Calling
        // Update() would force the brand new items to Modified and update rows that do not
        // exist yet, which surfaces as a phantom concurrency conflict.
        _mealLogRepoMock.Verify(r => r.Update(It.IsAny<MealLog>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_MissingMealLog_ThrowsKeyNotFoundException()
    {
        var mealLogId = Guid.NewGuid();
        _mealLogRepoMock.Setup(r => r.GetDetailedByIdAsync(mealLogId)).ReturnsAsync((MealLog?)null);

        var request = new LogMealRequest
        {
            UserId = Guid.NewGuid(),
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Dinner,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = Guid.NewGuid(), Quantity = 100f, Unit = "g" }
            }
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ExecuteAsync(mealLogId, request));
    }

    private static FoodItem CreateFoodItem(
        string name,
        float servingSize,
        float calories,
        float protein,
        float carbs,
        float fat)
    {
        return new FoodItem
        {
            Name = name,
            ServingSize = servingSize,
            ServingUnit = "g",
            NutritionalInfo = new NutritionalInfo(calories, protein, carbs, fat, 0f, 0f, 0f)
        };
    }

    /// <summary>
    /// BaseEntity assigns the id, so callers read it back off the entity rather than choosing
    /// it up front.
    /// </summary>
    private static MealLog CreateMealLog(
        Guid userId,
        FoodItem foodItem,
        float quantity,
        MealType mealType)
    {
        var mealLog = new MealLog
        {
            UserId = userId,
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = mealType
        };

        mealLog.Items.Add(new MealLogItem
        {
            MealLogId = mealLog.Id,
            FoodItemId = foodItem.Id,
            Quantity = quantity,
            Unit = "g",
            NutritionalInfo = foodItem.NutritionalInfo.Scale(quantity / foodItem.ServingSize),
            FoodItem = foodItem
        });

        return mealLog;
    }
}
