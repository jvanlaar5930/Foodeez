using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// The per-day half of MealPlanPrompt, which is how a plan is generated: one call per date
/// rather than one call for the week.
///
/// Two things here are guarantees rather than details. Slots that already have a meal in them
/// are named as fixed and excluded from what is asked for - a generator that talked the model
/// into replanning them would be a data-loss bug the moment it also stopped skipping them on
/// save. And ParseDay has to read what the model actually returns, which is not always the
/// shape it was asked for.
/// </summary>
public class MealPlanDayPromptTests
{
    private static readonly DateOnly Monday = new(2026, 1, 5);

    private static UserProfileDto Profile() => new()
    {
        UserId = Guid.NewGuid(),
        Age = 34,
        Gender = Gender.Female,
        WeightKg = 68,
        HeightCm = 168,
        DailyCalorieTarget = 2100,
        DailyProteinTargetG = 54.4f,
        DailyCarbTargetG = 259.6f,
        DailyFatTargetG = 73.4f,
        DietaryGoal = DietaryGoal.GeneralHealth,
        ActivityLevel = ActivityLevel.ModeratelyActive
    };

    private static GenerateMealPlanRequest Request() => new()
    {
        UserId = Guid.NewGuid(),
        StartDate = new DateOnly(2026, 1, 5),
        EndDate = new DateOnly(2026, 1, 11)
    };

    private static string BuildDay(
        GenerateMealPlanRequest? request = null,
        IReadOnlyCollection<string>? occupied = null,
        IReadOnlyCollection<string>? alreadyPlanned = null) =>
        MealPlanPrompt.BuildDay(
            request ?? Request(), Profile(), Monday, occupied ?? [], alreadyPlanned ?? []);

    // ── BuildDay ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildDay_AsksForOneNamedDay_WithItsWeekday()
    {
        var prompt = BuildDay();

        prompt.Should().Contain("2026-01-05");
        prompt.Should().Contain("Monday", because: "a model plans a Monday differently from a Saturday");
        prompt.Should().Contain("ONE day");
    }

    [Fact]
    public void BuildDay_AsksForTheSingleDayShape_NotTheWholePlanShape()
    {
        var prompt = BuildDay();

        prompt.Should().Contain("\"meals\"");
        prompt.Should().Contain("one day only, no \"days\" array");
    }

    [Fact]
    public void BuildDay_NamesTheSlotsAlreadyTaken_AndForbidsReplanningThem()
    {
        var prompt = BuildDay(occupied: ["Breakfast: Leftover pancakes"]);

        prompt.Should().Contain("Leftover pancakes");
        prompt.Should().Contain("staying exactly as it is");
        prompt.Should().Contain("Do not plan those meal types again");
    }

    [Fact]
    public void BuildDay_SaysNothingAboutOccupiedSlots_WhenTheDayIsEmpty()
    {
        BuildDay().Should().NotContain("staying exactly as it is");
    }

    [Fact]
    public void BuildDay_ListsWhatEarlierDaysOfTheSameRunProduced()
    {
        var prompt = BuildDay(alreadyPlanned: ["2026-01-04 Dinner: Chilli"]);

        prompt.Should().Contain("Chilli");
        prompt.Should().Contain("Already planned earlier in this same run");
        prompt.Should().Contain("Vary from these");
    }

    [Fact]
    public void BuildDay_KeepsTheExclusions_SoOneSlowDayCannotLoseAnAllergy()
    {
        var request = Request();
        request.ExcludeIngredients = ["peanuts"];

        BuildDay(request).Should().Contain("Exclude Ingredients: peanuts");
    }

    [Fact]
    public void BuildDay_CarriesGuidance_AndSubordinatesItToTheExclusions()
    {
        var request = Request();
        request.ExcludeIngredients = ["shellfish"];
        request.Guidance = "reuse last week's breakfasts";

        var prompt = BuildDay(request);

        prompt.Should().Contain("reuse last week's breakfasts");
        prompt.Should().Contain("those still hold, whatever it says");
    }

    [Fact]
    public void BuildDay_CarriesThePreviousPeriod_SoAReuseRequestHasSomethingToReuse()
    {
        var request = Request();
        request.Guidance = "same breakfasts as last week";
        request.PreviousPeriod = ["2025-12-29 Breakfast: Porridge"];

        BuildDay(request).Should().Contain("2025-12-29 Breakfast: Porridge");
    }

    [Fact]
    public void BuildDay_AsksForNarration_SoThereIsSomethingToWatchPerDay()
    {
        BuildDay().Should().Contain(AINarration.Instruction);
    }

    // ── ParseDay ─────────────────────────────────────────────────────────────

    [Fact]
    public void ParseDay_ReadsTheDayItWasAskedFor_AndStampsTheDateItself()
    {
        const string response = """
        Here is a balanced Monday.
        {"meals":[{"mealType":1,"recipeName":"Oatmeal","servings":1,"estimatedCalories":350,
          "ingredients":[{"name":"Rolled oats","quantity":80,"unit":"g"}]}]}
        """;

        var day = MealPlanPrompt.ParseDay(response, Monday);

        day.Date.Should().Be(Monday, because: "the model is not trusted to date its own answer");
        day.Meals.Should().HaveCount(1);
        day.Meals[0].RecipeName.Should().Be("Oatmeal");
        day.Meals[0].MealType.Should().Be(MealType.Breakfast);
        day.Meals[0].Ingredients.Should().ContainSingle().Which.Name.Should().Be("Rolled oats");
    }

    [Fact]
    public void ParseDay_AlsoReadsTheWholePlanShape_WhenAModelIgnoresTheInstruction()
    {
        // Asked for {"meals":[...]}, some models answer in the shape they know. That is still
        // an answer, and throwing it away would fail a day that had really been planned.
        const string response = """
        {"days":[{"date":"2026-01-05","meals":[{"mealType":5,"recipeName":"Chilli"}]}]}
        """;

        var day = MealPlanPrompt.ParseDay(response, Monday);

        day.Meals.Should().ContainSingle().Which.RecipeName.Should().Be("Chilli");
    }

    [Fact]
    public void ParseDay_ReturnsNoMeals_WhenNothingUsableCameBack()
    {
        MealPlanPrompt.ParseDay("The model is overloaded, try later.", Monday)
            .Meals.Should().BeEmpty(because: "an empty day is what the caller treats as a failed day");
    }

    [Fact]
    public void ParseDay_ReturnsNoMeals_ForAnAnswerCutOffMidJson()
    {
        MealPlanPrompt.ParseDay("""{"meals":[{"mealType":1,"recipeName":"Oat""", Monday)
            .Meals.Should().BeEmpty();
    }
}
