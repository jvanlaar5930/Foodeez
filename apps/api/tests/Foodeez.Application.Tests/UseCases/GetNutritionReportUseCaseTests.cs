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

/// <summary>
/// The report is what the charts are drawn from, and the judgement calls in it - what an
/// unlogged day counts as, what an average is averaged over - are the ones that decide whether
/// a reader is looking at their eating or at the gaps in their logging.
/// </summary>
public class GetNutritionReportUseCaseTests
{
    private static readonly DateOnly Start = new(2026, 3, 2);
    private static readonly DateOnly End = new(2026, 3, 8);

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMealLogRepository> _mealLogs = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly GetNutritionReportUseCase _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GetNutritionReportUseCaseTests()
    {
        _unitOfWork.Setup(u => u.MealLogs).Returns(_mealLogs.Object);
        _unitOfWork.Setup(u => u.Users).Returns(_users.Object);
        _sut = new GetNutritionReportUseCase(_unitOfWork.Object);
    }

    private void Logged(params MealLog[] logs) =>
        _mealLogs
            .Setup(r => r.GetByUserAndDateRangeAsync(_userId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(logs);

    private void WithCalorieTarget(int target)
    {
        var user = User.Create("test@test.com", "hash", "Test", "User");
        user.Profile = new UserProfile { UserId = user.Id, DailyCalorieTarget = target };
        _users.Setup(r => r.GetByIdAsync(_userId)).ReturnsAsync(user);
    }

    private static MealLog Meal(DateOnly date, MealType mealType, float calories, string food = "Porridge")
    {
        var log = new MealLog { UserId = Guid.NewGuid(), LogDate = date, MealType = mealType };

        log.Items.Add(new MealLogItem
        {
            MealLogId = log.Id,
            Quantity = 100f,
            Unit = "g",
            NutritionalInfo = new NutritionalInfo(calories, 10f, 20f, 5f, 3f, 2f, 100f),
            FoodItem = new FoodItem { Name = food, ServingSize = 100f, ServingUnit = "g" }
        });

        return log;
    }

    [Fact]
    public async Task EveryDayInTheRangeIsReturned_IncludingTheOnesWithNothingInThem()
    {
        Logged(Meal(Start, MealType.Breakfast, 500f));

        var report = await _sut.ExecuteAsync(_userId, Start, End);

        report.DaysInRange.Should().Be(7);
        report.Days.Should().HaveCount(7);
        report.Days.Select(day => day.Date).Should().BeInAscendingOrder();
        report.Days[0].HasLogs.Should().BeTrue();
        report.Days[1].HasLogs.Should().BeFalse();
        report.DaysLogged.Should().Be(1);
    }

    [Fact]
    public async Task AveragesCoverLoggedDaysOnly_SoAGapDoesNotReadAsADayOfEatingNothing()
    {
        Logged(
            Meal(Start, MealType.Breakfast, 400f),
            Meal(Start.AddDays(1), MealType.Dinner, 600f));

        var report = await _sut.ExecuteAsync(_userId, Start, End);

        // 1000 over the two logged days, not over the seven in the range.
        report.Averages.Calories.Should().Be(500f);
        report.Totals.Calories.Should().Be(1000f);
    }

    [Fact]
    public async Task ADayWithinATenthOfTheCalorieTargetCountsAsOnTarget()
    {
        WithCalorieTarget(2000);
        Logged(
            Meal(Start, MealType.Dinner, 1900f),           // 5% under - on target
            Meal(Start.AddDays(1), MealType.Dinner, 2200f), // 10% over - on target
            Meal(Start.AddDays(2), MealType.Dinner, 2600f)); // 30% over - not

        var report = await _sut.ExecuteAsync(_userId, Start, End);

        report.DaysOnTarget.Should().Be(2);
    }

    [Fact]
    public async Task MealsAreGroupedByTypeAndFoodsByName()
    {
        Logged(
            Meal(Start, MealType.Breakfast, 300f, food: "Porridge"),
            Meal(Start.AddDays(1), MealType.Breakfast, 350f, food: "porridge"),
            Meal(Start.AddDays(1), MealType.Dinner, 700f, food: "Chilli"));

        var report = await _sut.ExecuteAsync(_userId, Start, End);

        report.ByMealType.Should().HaveCount(2);
        report.ByMealType.Single(entry => entry.MealType == MealType.Breakfast).Calories.Should().Be(650f);

        // The same food logged under two spellings is one food to the reader.
        var top = report.TopFoods.First();
        top.Name.Should().Be("Porridge");
        top.TimesLogged.Should().Be(2);
    }

    [Fact]
    public async Task ABackwardsRangeIsRefused()
    {
        var act = () => _sut.ExecuteAsync(_userId, End, Start);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ARangeLongerThanAYearIsRefused()
    {
        var act = () => _sut.ExecuteAsync(_userId, Start, Start.AddDays(GetNutritionReportUseCase.MaxDays));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
