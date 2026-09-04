using System.Reflection;
using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Domain.ValueObjects;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// The seven-field copy this replaces was written out nine times. Adding an eighth nutrient
/// meant finding all nine, and the one that got missed would have reported zero rather than
/// failing.
/// </summary>
public class NutritionMapperTests
{
    [Fact]
    public void ToDto_CarriesEveryField()
    {
        var nutrition = new NutritionalInfo(520f, 31f, 44f, 22f, 6f, 9f, 810f);

        var dto = NutritionMapper.ToDto(nutrition);

        foreach (var property in typeof(NutritionalInfoDto).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var source = typeof(NutritionalInfo).GetProperty(property.Name);
            source.Should().NotBeNull($"NutritionalInfoDto.{property.Name} has nothing to map from");
            property.GetValue(dto).Should().Be(source!.GetValue(nutrition),
                $"NutritionMapper.ToDto does not copy {property.Name}");
        }
    }

    [Fact]
    public void ToDto_DoesNotMixUpCarbsAndFat()
    {
        // Distinct values, so a transposed pair of lines is visible rather than averaging out.
        var dto = NutritionMapper.ToDto(new NutritionalInfo(1f, 2f, 3f, 4f, 5f, 6f, 7f));

        dto.Calories.Should().Be(1f);
        dto.Protein.Should().Be(2f);
        dto.Carbohydrates.Should().Be(3f);
        dto.Fat.Should().Be(4f);
        dto.Fiber.Should().Be(5f);
        dto.Sugar.Should().Be(6f);
        dto.Sodium.Should().Be(7f);
    }
}
