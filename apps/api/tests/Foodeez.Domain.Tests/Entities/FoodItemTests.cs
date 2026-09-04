using FluentAssertions;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;
using Xunit;

namespace Foodeez.Domain.Tests.Entities;

/// <summary>
/// Scaling a food's label figures to the amount actually eaten was written out at three call
/// sites, including the rule for a food with no serving size recorded. That rule is exactly
/// the sort of thing that gets copied as two of the three.
/// </summary>
public class FoodItemTests
{
    private static FoodItem PerHundredGrams(float servingSize = 100f) => new()
    {
        Name = "Rolled oats",
        ServingSize = servingSize,
        ServingUnit = "g",
        NutritionalInfo = new NutritionalInfo(380f, 13f, 67f, 7f, 10f, 1f, 5f)
    };

    [Fact]
    public void OneServing_IsTheLabelUnchanged()
    {
        var nutrition = PerHundredGrams().NutritionFor(100f);

        nutrition.Calories.Should().Be(380f);
        nutrition.Protein.Should().Be(13f);
    }

    [Fact]
    public void MoreThanAServing_ScalesUp()
    {
        var nutrition = PerHundredGrams().NutritionFor(150f);

        nutrition.Calories.Should().BeApproximately(570f, 0.01f);
        nutrition.Sodium.Should().BeApproximately(7.5f, 0.01f);
    }

    [Fact]
    public void LessThanAServing_ScalesDown()
    {
        PerHundredGrams().NutritionFor(40f).Calories.Should().BeApproximately(152f, 0.01f);
    }

    [Fact]
    public void NoneOfIt_IsNothing()
    {
        PerHundredGrams().NutritionFor(0f).Calories.Should().Be(0f);
    }

    [Fact]
    public void WithNoServingSizeRecorded_TheQuantityIsTheMultiplier()
    {
        // Hand-entered foods can arrive without one. Dividing by zero would make every figure
        // infinite, and treating it as one serving would ignore the amount entirely.
        var nutrition = PerHundredGrams(servingSize: 0f).NutritionFor(3f);

        nutrition.Calories.Should().BeApproximately(1140f, 0.01f);
    }

    [Fact]
    public void TheLabelItselfIsNotChanged()
    {
        var item = PerHundredGrams();

        item.NutritionFor(250f);

        item.NutritionalInfo.Calories.Should().Be(380f, because: "scaling returns a new value");
    }
}
