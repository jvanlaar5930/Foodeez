using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Application.UseCases.MealLogs;
using Foodeez.Domain.Entities;
using Foodeez.Domain.Enums;
using Foodeez.Domain.ValueObjects;
using Moq;
using Xunit;

namespace Foodeez.Application.Tests.UseCases;

public class GetNutritionSummaryUseCaseTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMealLogRepository> _mealLogRepoMock;
    private readonly Mock<IUserRepository> _userRepoMock;
    private readonly GetNutritionSummaryUseCase _sut;

    public GetNutritionSummaryUseCaseTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mealLogRepoMock = new Mock<IMealLogRepository>();
        _userRepoMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(u => u.MealLogs).Returns(_mealLogRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);

        _sut = new GetNutritionSummaryUseCase(_unitOfWorkMock.Object);
    }

    private User CreateUserWithProfile(int calorieTarget = 2000, float proteinTarget = 150f,
        float carbTarget = 250f, float fatTarget = 65f)
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");
        user.Profile = new UserProfile
        {
            UserId = user.Id,
            DailyCalorieTarget = calorieTarget,
            DailyProteinTargetG = proteinTarget,
            DailyCarbTargetG = carbTarget,
            DailyFatTargetG = fatTarget,
            ProfileCompleted = true
        };
        return user;
    }

    private MealLog CreateMealLog(Guid userId, MealType mealType,
        float calories, float protein, float carbs, float fat, float fiber = 5f)
    {
        var foodItem = new FoodItem
        {
            Name = "Test Food",
            ServingSize = 100f,
            ServingUnit = "g",
            NutritionalInfo = new NutritionalInfo(calories, protein, carbs, fat, fiber, 2f, 100f)
        };

        var log = new MealLog
        {
            UserId = userId,
            LogDate = DateOnly.FromDateTime(DateTime.UtcNow),
            MealType = mealType
        };

        var item = new MealLogItem
        {
            MealLogId = log.Id,
            FoodItemId = foodItem.Id,
            Quantity = 100f,
            Unit = "g",
            NutritionalInfo = new NutritionalInfo(calories, protein, carbs, fat, fiber, 2f, 100f),
            FoodItem = foodItem
        };

        log.Items.Add(item);
        return log;
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesPercentagesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUserWithProfile(calorieTarget: 2000, proteinTarget: 100f, carbTarget: 200f, fatTarget: 80f);

        var logs = new List<MealLog>
        {
            CreateMealLog(userId, MealType.Breakfast, calories: 1000f, protein: 50f, carbs: 100f, fat: 40f)
        };

        _mealLogRepoMock.Setup(r => r.GetByUserAndDateAsync(userId, today))
            .ReturnsAsync(logs.AsReadOnly());
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var summary = await _sut.ExecuteAsync(userId, today);

        // Assert
        summary.TotalCalories.Should().BeApproximately(1000f, 0.1f);
        summary.CaloriesPercentage.Should().BeApproximately(50f, 0.1f);   // 1000/2000 = 50%
        summary.ProteinPercentage.Should().BeApproximately(50f, 0.1f);    // 50/100 = 50%
        summary.CarbsPercentage.Should().BeApproximately(50f, 0.1f);      // 100/200 = 50%
        summary.FatPercentage.Should().BeApproximately(50f, 0.1f);        // 40/80 = 50%
    }

    [Fact]
    public async Task ExecuteAsync_ZeroTargets_DoesNotThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUserWithProfile(calorieTarget: 0, proteinTarget: 0f, carbTarget: 0f, fatTarget: 0f);

        _mealLogRepoMock.Setup(r => r.GetByUserAndDateAsync(userId, today))
            .ReturnsAsync(new List<MealLog>().AsReadOnly());
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var act = () => _sut.ExecuteAsync(userId, today);

        // Assert — should not throw
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_MultipleMealTypes_AggregatesAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUserWithProfile(calorieTarget: 2500);

        var logs = new List<MealLog>
        {
            CreateMealLog(userId, MealType.Breakfast, calories: 400f, protein: 20f, carbs: 50f, fat: 15f),
            CreateMealLog(userId, MealType.Lunch, calories: 600f, protein: 35f, carbs: 70f, fat: 20f),
            CreateMealLog(userId, MealType.Dinner, calories: 800f, protein: 45f, carbs: 90f, fat: 25f),
            CreateMealLog(userId, MealType.AfternoonSnack, calories: 200f, protein: 10f, carbs: 25f, fat: 8f)
        };

        _mealLogRepoMock.Setup(r => r.GetByUserAndDateAsync(userId, today))
            .ReturnsAsync(logs.AsReadOnly());
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var summary = await _sut.ExecuteAsync(userId, today);

        // Assert
        summary.TotalCalories.Should().BeApproximately(2000f, 0.5f);
        summary.TotalProtein.Should().BeApproximately(110f, 0.5f);
        summary.TotalCarbs.Should().BeApproximately(235f, 0.5f);
        summary.TotalFat.Should().BeApproximately(68f, 0.5f);
    }

    [Fact]
    public async Task ExecuteAsync_NoLogs_ReturnsSummaryWithZeroTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var user = CreateUserWithProfile();

        _mealLogRepoMock.Setup(r => r.GetByUserAndDateAsync(userId, today))
            .ReturnsAsync(new List<MealLog>().AsReadOnly());
        _userRepoMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var summary = await _sut.ExecuteAsync(userId, today);

        // Assert
        summary.TotalCalories.Should().Be(0f);
        summary.TotalProtein.Should().Be(0f);
        summary.CaloriesPercentage.Should().Be(0f);
    }
}
