using System.Text.Json;
using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// Models write this field however they like, and the meal-plan JSON shape has been asking
/// them inconsistently - two of the six copies of that shape say `"mealType": 1` and four say
/// `"mealType":"Breakfast"`. Phase 3 collapses those to one, and this is what says the reader
/// still copes with everything the old ones invited.
/// </summary>
public class MealTypeParsingTests
{
    [Theory]
    [InlineData(1, MealType.Breakfast)]
    [InlineData(2, MealType.MorningSnack)]
    [InlineData(3, MealType.Lunch)]
    [InlineData(4, MealType.AfternoonSnack)]
    [InlineData(5, MealType.Dinner)]
    [InlineData(6, MealType.EveningSnack)]
    public void Read_NumericForm_MapsToTheMember(int value, MealType expected)
    {
        Read(value.ToString()).Should().Be(expected);
    }

    [Theory]
    [InlineData("\"3\"", MealType.Lunch)]
    [InlineData("\"Lunch\"", MealType.Lunch)]
    [InlineData("\"lunch\"", MealType.Lunch)]
    [InlineData("\"LUNCH\"", MealType.Lunch)]
    public void Read_StringForms_MapToTheMember(string json, MealType expected)
    {
        Read(json).Should().Be(expected);
    }

    /// <summary>
    /// The short labels the calendar itself shows. A model that has seen them, or that simply
    /// reaches for the common wording, used to land on the fallback - which is Breakfast, so an
    /// "AM Snack" became a breakfast without anything saying so.
    /// </summary>
    [Theory]
    [InlineData("\"AM Snack\"", MealType.MorningSnack)]
    [InlineData("\"am snack\"", MealType.MorningSnack)]
    [InlineData("\"PM Snack\"", MealType.AfternoonSnack)]
    [InlineData("\"Evening\"", MealType.EveningSnack)]
    public void Read_TheAppsOwnShortLabels_LandOnTheRightSlot(string json, MealType expected)
    {
        Read(json).Should().Be(expected);
    }

    [Theory]
    [InlineData("\"afternoon_snack\"")]
    [InlineData("\"afternoon-snack\"")]
    [InlineData("\"Afternoon Snack\"")]
    [InlineData("\"AfternoonSnack\"")]
    public void Read_SeparatorVariants_AllLandOnTheSameMember(string json)
    {
        Read(json).Should().Be(MealType.AfternoonSnack);
    }

    [Fact]
    public void Read_BareSnack_IsTreatedAsAfternoonSnack()
    {
        // Common in model output and has no exact member of its own.
        Read("\"snack\"").Should().Be(MealType.AfternoonSnack);
        Read("\"Snack\"").Should().Be(MealType.AfternoonSnack);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData("-1")]
    [InlineData("\"9\"")]
    public void Read_OutOfRangeNumber_FallsBackRatherThanCastingBlindly(string json)
    {
        // (MealType)7 would be a value no switch handles; the fallback is the safe answer.
        Read(json).Should().Be(MealType.Breakfast);
    }

    [Theory]
    [InlineData("\"brunch\"")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Read_UnrecognisedOrWrongType_UsesTheFallback(string json)
    {
        Read(json).Should().Be(MealType.Breakfast);
    }

    [Fact]
    public void Read_HonoursACallerSuppliedFallback()
    {
        Read("\"brunch\"", MealType.Dinner).Should().Be(MealType.Dinner);
    }

    [Fact]
    public void Read_NeverThrows_ForAnyShapeAModelMightEmit()
    {
        var shapes = new[] { "1", "\"Lunch\"", "\"nonsense\"", "null", "true", "[]", "{}", "1.5", "\"\"" };

        foreach (var shape in shapes)
        {
            var act = () => Read(shape);
            act.Should().NotThrow(because: $"a model may emit {shape}, and throwing discards the whole plan");
        }
    }

    private static MealType Read(string json, MealType fallback = MealType.Breakfast)
    {
        using var doc = JsonDocument.Parse(json);
        return MealTypeParsing.Read(doc.RootElement, fallback);
    }
}
