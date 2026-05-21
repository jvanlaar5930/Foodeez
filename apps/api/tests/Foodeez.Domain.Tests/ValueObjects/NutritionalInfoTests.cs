using FluentAssertions;
using Foodeez.Domain.ValueObjects;
using Xunit;

namespace Foodeez.Domain.Tests.ValueObjects;

public class NutritionalInfoTests
{
    private static NutritionalInfo CreateSample(
        float calories = 200f, float protein = 10f, float carbs = 25f,
        float fat = 8f, float fiber = 3f, float sugar = 5f, float sodium = 150f)
        => new NutritionalInfo(calories, protein, carbs, fat, fiber, sugar, sodium);

    [Fact]
    public void Empty_ReturnsAllZeros()
    {
        var empty = NutritionalInfo.Empty;

        empty.Calories.Should().Be(0f);
        empty.Protein.Should().Be(0f);
        empty.Carbohydrates.Should().Be(0f);
        empty.Fat.Should().Be(0f);
        empty.Fiber.Should().Be(0f);
        empty.Sugar.Should().Be(0f);
        empty.Sodium.Should().Be(0f);
    }

    [Fact]
    public void Scale_ByFactor_MultipliesAllValues()
    {
        // Arrange
        var info = CreateSample(calories: 100, protein: 10, carbs: 20, fat: 5, fiber: 2, sugar: 3, sodium: 100);

        // Act
        var scaled = info.Scale(2.5f);

        // Assert
        scaled.Calories.Should().BeApproximately(250f, 0.01f);
        scaled.Protein.Should().BeApproximately(25f, 0.01f);
        scaled.Carbohydrates.Should().BeApproximately(50f, 0.01f);
        scaled.Fat.Should().BeApproximately(12.5f, 0.01f);
        scaled.Fiber.Should().BeApproximately(5f, 0.01f);
        scaled.Sugar.Should().BeApproximately(7.5f, 0.01f);
        scaled.Sodium.Should().BeApproximately(250f, 0.01f);
    }

    [Fact]
    public void Scale_ByZero_ReturnsAllZeros()
    {
        var info = CreateSample();
        var scaled = info.Scale(0f);

        scaled.Calories.Should().Be(0f);
        scaled.Protein.Should().Be(0f);
    }

    [Fact]
    public void Scale_ByOne_ReturnsSameValues()
    {
        var info = CreateSample(calories: 300, protein: 15);
        var scaled = info.Scale(1f);

        scaled.Calories.Should().BeApproximately(300f, 0.01f);
        scaled.Protein.Should().BeApproximately(15f, 0.01f);
    }

    [Fact]
    public void Scale_DoesNotMutateOriginal()
    {
        var info = CreateSample(calories: 100);
        _ = info.Scale(3f);

        info.Calories.Should().Be(100f);
    }

    [Fact]
    public void Addition_SumsBothInstances()
    {
        // Arrange
        var a = CreateSample(calories: 300, protein: 20, carbs: 40, fat: 10, fiber: 5, sugar: 8, sodium: 200);
        var b = CreateSample(calories: 150, protein: 10, carbs: 20, fat: 5, fiber: 3, sugar: 4, sodium: 100);

        // Act
        var result = a + b;

        // Assert
        result.Calories.Should().BeApproximately(450f, 0.01f);
        result.Protein.Should().BeApproximately(30f, 0.01f);
        result.Carbohydrates.Should().BeApproximately(60f, 0.01f);
        result.Fat.Should().BeApproximately(15f, 0.01f);
        result.Fiber.Should().BeApproximately(8f, 0.01f);
        result.Sugar.Should().BeApproximately(12f, 0.01f);
        result.Sodium.Should().BeApproximately(300f, 0.01f);
    }

    [Fact]
    public void Addition_WithEmpty_ReturnsSameValues()
    {
        var info = CreateSample(calories: 500, protein: 25);
        var result = info + NutritionalInfo.Empty;

        result.Calories.Should().BeApproximately(500f, 0.01f);
        result.Protein.Should().BeApproximately(25f, 0.01f);
    }

    [Fact]
    public void Addition_DoesNotMutateOperands()
    {
        var a = CreateSample(calories: 100);
        var b = CreateSample(calories: 200);
        _ = a + b;

        a.Calories.Should().Be(100f);
        b.Calories.Should().Be(200f);
    }

    [Fact]
    public void Scale_ThenAdd_ProducesCorrectAggregate()
    {
        // Simulate two servings of the same food
        var perServing = CreateSample(calories: 200, protein: 10);
        var serving1 = perServing.Scale(1f);
        var serving2 = perServing.Scale(1.5f);
        var total = serving1 + serving2;

        total.Calories.Should().BeApproximately(500f, 0.01f);
        total.Protein.Should().BeApproximately(25f, 0.01f);
    }
}
