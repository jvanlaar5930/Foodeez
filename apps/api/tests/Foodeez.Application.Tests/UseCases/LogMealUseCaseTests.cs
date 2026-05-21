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

public class LogMealUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepoMock;
    private readonly Mock<IMealLogRepository> _mealLogRepoMock;
    private readonly LogMealUseCase _sut;

    public LogMealUseCaseTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _foodItemRepoMock = new Mock<IFoodItemRepository>();
        _mealLogRepoMock = new Mock<IMealLogRepository>();

        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.MealLogs).Returns(_mealLogRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _sut = new LogMealUseCase(_unitOfWorkMock.Object);
    }

    private FoodItem CreateFoodItem(
        string name = "Chicken Breast",
        float servingSize = 100f,
        float calories = 165f,
        float protein = 31f,
        float carbs = 0f,
        float fat = 3.6f)
    {
        var item = new FoodItem
        {
            Name = name,
            ServingSize = servingSize,
            ServingUnit = "g",
            NutritionalInfo = new NutritionalInfo(calories, protein, carbs, fat, 0f, 0f, 74f)
        };
        return item;
    }

    [Fact]
    public async Task ExecuteAsync_ValidRequest_CreatesMealLog()
    {
        // Arrange
        var foodItemId = Guid.NewGuid();
        var foodItem = CreateFoodItem();
        _foodItemRepoMock.Setup(r => r.GetByIdAsync(foodItemId)).ReturnsAsync(foodItem);

        var request = new LogMealRequest
        {
            UserId = Guid.NewGuid(),
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Lunch,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = foodItemId, Quantity = 150f, Unit = "g" }
            }
        };

        MealLog? capturedLog = null;
        _mealLogRepoMock.Setup(r => r.AddAsync(It.IsAny<MealLog>()))
            .Callback<MealLog>(log => capturedLog = log)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.MealType.Should().Be(MealType.Lunch);
        result.Items.Should().HaveCount(1);
        _mealLogRepoMock.Verify(r => r.AddAsync(It.IsAny<MealLog>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ItemsHaveScaledNutrition()
    {
        // Arrange — food item has 165 kcal per 100g, we log 200g => 330 kcal
        var foodItemId = Guid.NewGuid();
        var foodItem = CreateFoodItem(servingSize: 100f, calories: 165f, protein: 31f);
        _foodItemRepoMock.Setup(r => r.GetByIdAsync(foodItemId)).ReturnsAsync(foodItem);

        var request = new LogMealRequest
        {
            UserId = Guid.NewGuid(),
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Dinner,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = foodItemId, Quantity = 200f, Unit = "g" }
            }
        };

        _mealLogRepoMock.Setup(r => r.AddAsync(It.IsAny<MealLog>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert — 200g / 100g = scale factor 2.0
        result.Items.Should().HaveCount(1);
        result.Items[0].NutritionalInfo.Calories.Should().BeApproximately(330f, 0.1f);
        result.Items[0].NutritionalInfo.Protein.Should().BeApproximately(62f, 0.1f);
    }

    [Fact]
    public async Task ExecuteAsync_FoodItemNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _foodItemRepoMock.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((FoodItem?)null);

        var request = new LogMealRequest
        {
            UserId = Guid.NewGuid(),
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Breakfast,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = missingId, Quantity = 100f, Unit = "g" }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ExecuteAsync(request));
    }

    [Fact]
    public async Task ExecuteAsync_MultipleItems_TotalNutritionAggregatesCorrectly()
    {
        // Arrange
        var foodId1 = Guid.NewGuid();
        var foodId2 = Guid.NewGuid();

        _foodItemRepoMock.Setup(r => r.GetByIdAsync(foodId1))
            .ReturnsAsync(CreateFoodItem("Item1", servingSize: 100, calories: 100, protein: 10));
        _foodItemRepoMock.Setup(r => r.GetByIdAsync(foodId2))
            .ReturnsAsync(CreateFoodItem("Item2", servingSize: 100, calories: 200, protein: 20));

        var request = new LogMealRequest
        {
            UserId = Guid.NewGuid(),
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = MealType.Breakfast,
            Items = new List<MealLogItemRequest>
            {
                new() { FoodItemId = foodId1, Quantity = 100f, Unit = "g" },
                new() { FoodItemId = foodId2, Quantity = 100f, Unit = "g" }
            }
        };

        _mealLogRepoMock.Setup(r => r.AddAsync(It.IsAny<MealLog>())).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(request);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalNutrition.Calories.Should().BeApproximately(300f, 0.1f);
        result.TotalNutrition.Protein.Should().BeApproximately(30f, 0.1f);
    }
}
