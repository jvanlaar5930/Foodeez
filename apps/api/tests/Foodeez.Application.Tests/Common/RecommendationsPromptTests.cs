using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// Recommendations were the last feature still building their prompt inside each provider, and
/// the five copies had drifted badly: Claude's sent the full profile and a week of recent
/// nutrition, while Groq's and Ollama's were one interpolated line with no nutrition at all -
/// so the same user got materially worse advice depending on which provider was selected.
/// None of them mentioned excluded foods.
/// </summary>
public class RecommendationsPromptTests
{
    private static UserProfileDto Profile() => new()
    {
        UserId = Guid.NewGuid(),
        Age = 41,
        Gender = Gender.Female,
        HeightCm = 168,
        WeightKg = 68,
        DailyCalorieTarget = 2100,
        DailyProteinTargetG = 54.4f,
        DailyCarbTargetG = 259.6f,
        DailyFatTargetG = 73.4f,
        DietaryGoal = DietaryGoal.WeightLoss,
        ActivityLevel = ActivityLevel.LightlyActive
    };

    // ── Build ────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_CarriesTheWholeProfile()
    {
        var prompt = RecommendationsPrompt.Build(Profile());

        prompt.Should().Contain("41");
        prompt.Should().Contain("168");
        prompt.Should().Contain("68");
        prompt.Should().Contain("2100");
        prompt.Should().Contain(nameof(DietaryGoal.WeightLoss));
        prompt.Should().Contain(nameof(ActivityLevel.LightlyActive));
    }

    [Fact]
    public void Build_IncludesRecentNutritionWhenThereIsSome()
    {
        var recent = new DailyNutritionDto
        {
            TotalCalories = 1850,
            TargetCalories = 2100,
            TotalProtein = 61,
            TotalCarbs = 190,
            TotalFat = 70,
            TotalFiber = 22
        };

        var prompt = RecommendationsPrompt.Build(Profile(), recent);

        prompt.Should().Contain("Recent Average Daily Nutrition");
        prompt.Should().Contain("1850");
        prompt.Should().Contain("22");
    }

    [Fact]
    public void Build_OmitsTheNutritionSectionWhenThereIsNone()
    {
        RecommendationsPrompt.Build(Profile()).Should().NotContain("Recent Average Daily Nutrition");
    }

    [Fact]
    public void Build_StatesExcludedFoodsAsAHardConstraint()
    {
        // No provider's own prompt mentioned these, so recommendations could suggest a food
        // the user had told us they cannot eat.
        var profile = Profile();
        profile.ExcludedFoods = ["peanuts", "shellfish"];

        var prompt = RecommendationsPrompt.Build(profile);

        prompt.Should().Contain("peanuts");
        prompt.Should().Contain("shellfish");
        prompt.Should().Contain("never suggest them");
    }

    [Fact]
    public void Build_OmitsTheExclusionLineWhenThereAreNone()
    {
        RecommendationsPrompt.Build(Profile()).Should().NotContain("cannot eat");
    }

    [Fact]
    public void Build_OmitsTargetWeightWhenItIsNotSet()
    {
        var withTarget = Profile();
        withTarget.TargetWeightKg = 62;

        RecommendationsPrompt.Build(withTarget).Should().Contain("Target Weight");
        RecommendationsPrompt.Build(Profile()).Should().NotContain("Target Weight");
    }

    // ── Parse ────────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ReadsEveryField()
    {
        var response = """
            Here is how the week looks.

            {
              "overallScore": 82,
              "suggestions": ["Eat more fibre", "Add a protein source at breakfast"],
              "deficiencies": ["Fibre"],
              "tips": ["Batch-cook on Sunday"]
            }
            """;

        var result = RecommendationsPrompt.Parse(response)!;

        result.OverallScore.Should().Be(82);
        result.Suggestions.Should().Equal("Eat more fibre", "Add a protein source at breakfast");
        result.Deficiencies.Should().Equal("Fibre");
        result.Tips.Should().Equal("Batch-cook on Sunday");
    }

    [Fact]
    public void Parse_MissingScore_UsesTheNeutralMiddleRatherThanZero()
    {
        // Zero would read as a damning verdict on someone's eating; the middle reads as
        // "we don't know", which is what actually happened.
        RecommendationsPrompt.Parse("""{"suggestions":[]}""")!.OverallScore.Should().Be(50);
    }

    [Theory]
    [InlineData("")]
    [InlineData("I can't help with that.")]
    [InlineData("""{"overallScore": 70""")]
    public void Parse_UnusableResponses_ReturnNull(string response)
    {
        // Null is what tells the caller to use the fallback, rather than showing an empty page.
        RecommendationsPrompt.Parse(response).Should().BeNull();
    }

    [Fact]
    public void Parse_ToleratesAQuotedScore()
    {
        RecommendationsPrompt.Parse("""{"overallScore":"64"}""")!.OverallScore.Should().Be(64);
    }

    // ── Fallback ─────────────────────────────────────────────────────────────

    [Fact]
    public void Fallback_IsBuiltFromTheProfile_NotLeftEmpty()
    {
        // Four of the five providers used to return a blank result on an outage, so the page
        // showed nothing at all. None of this needs a model.
        var result = RecommendationsPrompt.Fallback(Profile());

        result.OverallScore.Should().Be(50);
        result.Suggestions.Should().NotBeEmpty();
        result.Tips.Should().NotBeEmpty();
        result.Suggestions.Should().Contain(s => s.Contains("2100"));
        result.Suggestions.Should().Contain(s => s.Contains("54.4"));
    }
}
