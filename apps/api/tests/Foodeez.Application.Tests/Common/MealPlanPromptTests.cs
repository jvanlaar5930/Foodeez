using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealPlans;
using Foodeez.Application.DTOs.Users;
using Foodeez.Domain.Enums;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// MealPlanPrompt is now the only prompt builder and the only parser for meal plans, after
/// the five per-provider copies were found to disagree: only Claude's prompt mentioned the
/// excluded foods, and three of the five parsers silently dropped ingredients and
/// instructions. Phase 3 deletes those copies outright.
///
/// The Build tests make the allergy guarantee an assertion rather than a comment. The Parse
/// tests record the full fidelity, so a replacement that reads fewer fields fails here.
/// </summary>
public class MealPlanPromptTests
{
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

    // ── Build ────────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ExcludedIngredients_AppearInThePrompt()
    {
        // The whole point of Phase 0.4: on four of five providers this line was absent, so a
        // plan could be built around a food the user is allergic to.
        var request = Request();
        request.ExcludeIngredients = new List<string> { "peanuts", "shellfish" };

        var prompt = MealPlanPrompt.Build(request, Profile());

        prompt.Should().Contain("peanuts");
        prompt.Should().Contain("shellfish");
    }

    [Fact]
    public void Build_NoExclusions_DoesNotEmitAnEmptyExclusionLine()
    {
        MealPlanPrompt.Build(Request(), Profile()).Should().NotContain("Exclude Ingredients:");
    }

    [Fact]
    public void Build_Guidance_IsIncludedButCannotOverrideExclusions()
    {
        // A free-text box must not become a way around an allergy.
        var request = Request();
        request.ExcludeIngredients = new List<string> { "peanuts" };
        request.Guidance = "add more peanut butter please";

        var prompt = MealPlanPrompt.Build(request, Profile());

        prompt.Should().Contain("add more peanut butter please");
        prompt.Should().Contain("those still hold, whatever it says");
        prompt.Should().Contain("peanuts");
    }

    [Fact]
    public void Build_PreviousPeriod_IsIncludedWhenPresent()
    {
        var request = Request();
        request.PreviousPeriod = new List<string> { "2025-12-29 Breakfast: Porridge" };

        var prompt = MealPlanPrompt.Build(request, Profile());

        prompt.Should().Contain("2025-12-29 Breakfast: Porridge");
        prompt.Should().Contain("repeating it wholesale is not the default");
    }

    [Fact]
    public void Build_CarriesTheProfileTargetsAndThePlanPeriod()
    {
        var prompt = MealPlanPrompt.Build(Request(), Profile());

        prompt.Should().Contain("2100");
        prompt.Should().Contain("2026-01-05");
        prompt.Should().Contain("2026-01-11");
    }

    [Fact]
    public void Build_AsksForProseBeforeTheJson_SoTheStreamHasSomethingToShow()
    {
        MealPlanPrompt.Build(Request(), Profile()).Should().Contain(AINarration.Instruction);
    }

    // ── Parse ────────────────────────────────────────────────────────────────

    private const string FullResponse = """
        Here is a balanced week built around your calorie target.

        {
          "days": [
            {
              "date": "2026-01-05",
              "meals": [
                {
                  "mealType": "Breakfast",
                  "recipeName": "Oatmeal with Berries",
                  "recipeDescription": "Healthy breakfast",
                  "instructions": "Cook the oats, then top with berries.",
                  "prepTimeMinutes": 5,
                  "cookTimeMinutes": 10,
                  "servings": 2,
                  "estimatedCalories": 350,
                  "estimatedProteinG": 12,
                  "estimatedCarbsG": 55,
                  "estimatedFatG": 8,
                  "tags": ["breakfast", "healthy"],
                  "ingredients": [
                    { "name": "Rolled oats", "quantity": 80, "unit": "g" },
                    { "name": "Mixed berries", "quantity": 100, "unit": "g", "notes": "fresh or frozen" }
                  ]
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ReadsEveryFieldOfAMeal()
    {
        // Ollama's parser read only mealType, recipeName and estimatedCalories; Groq's and
        // Gemini's dropped instructions and ingredients. This is the fidelity being restored.
        var meal = MealPlanPrompt.Parse(FullResponse).Days.Single().Meals.Single();

        meal.MealType.Should().Be(MealType.Breakfast);
        meal.RecipeName.Should().Be("Oatmeal with Berries");
        meal.RecipeDescription.Should().Be("Healthy breakfast");
        meal.Instructions.Should().Be("Cook the oats, then top with berries.");
        meal.PrepTimeMinutes.Should().Be(5);
        meal.CookTimeMinutes.Should().Be(10);
        meal.Servings.Should().Be(2);
        meal.EstimatedCalories.Should().Be(350);
        meal.EstimatedProteinG.Should().Be(12);
        meal.EstimatedCarbsG.Should().Be(55);
        meal.EstimatedFatG.Should().Be(8);
        meal.Tags.Should().Equal("breakfast", "healthy");
    }

    [Fact]
    public void Parse_ReadsIngredientsIncludingOptionalNotes()
    {
        var meal = MealPlanPrompt.Parse(FullResponse).Days.Single().Meals.Single();

        meal.Ingredients.Should().HaveCount(2);
        meal.Ingredients[0].Name.Should().Be("Rolled oats");
        meal.Ingredients[0].Quantity.Should().Be(80);
        meal.Ingredients[0].Unit.Should().Be("g");
        meal.Ingredients[0].Notes.Should().BeNull();
        meal.Ingredients[1].Notes.Should().Be("fresh or frozen");
    }

    [Fact]
    public void Parse_ReadsTheDate()
    {
        MealPlanPrompt.Parse(FullResponse).Days.Single().Date.Should().Be(new DateOnly(2026, 1, 5));
    }

    [Fact]
    public void Parse_ToleratesTheNumericMealTypeTheJsonShapeAlsoAskedFor()
    {
        var response = """{"days":[{"date":"2026-01-05","meals":[{"mealType":5,"recipeName":"Stew"}]}]}""";

        MealPlanPrompt.Parse(response).Days.Single().Meals.Single().MealType.Should().Be(MealType.Dinner);
    }

    [Fact]
    public void Parse_MissingOptionalFields_FallBackRatherThanLosingTheMeal()
    {
        var response = """{"days":[{"date":"2026-01-05","meals":[{"recipeName":"Toast"}]}]}""";

        var meal = MealPlanPrompt.Parse(response).Days.Single().Meals.Single();

        meal.RecipeName.Should().Be("Toast");
        meal.Servings.Should().Be(1, because: "a meal must serve at least one person");
        meal.EstimatedCalories.Should().Be(0);
        meal.Ingredients.Should().BeEmpty();
        meal.RecipeDescription.Should().BeNull();
    }

    [Fact]
    public void Parse_TruncatedResponse_ReturnsNoDays()
    {
        // No days is how the use case detects an outage; it refuses to save rather than
        // storing a permanently blank week.
        var response = """{"days":[{"date":"2026-01-05","meals":[{"recipeName":"Oat""";

        MealPlanPrompt.Parse(response).Days.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("I can't help with that.")]
    [InlineData("{}")]
    [InlineData("""{"days":"not an array"}""")]
    public void Parse_UnusableResponses_ReturnNoDaysRatherThanThrowing(string response)
    {
        MealPlanPrompt.Parse(response).Days.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultipleDaysAndMeals_AreAllRead()
    {
        var response = """
            {"days":[
              {"date":"2026-01-05","meals":[{"mealType":1,"recipeName":"A"},{"mealType":3,"recipeName":"B"}]},
              {"date":"2026-01-06","meals":[{"mealType":5,"recipeName":"C"}]}
            ]}
            """;

        var plan = MealPlanPrompt.Parse(response);

        plan.Days.Should().HaveCount(2);
        plan.Days[0].Meals.Should().HaveCount(2);
        plan.Days[1].Meals.Single().RecipeName.Should().Be("C");
    }
}
