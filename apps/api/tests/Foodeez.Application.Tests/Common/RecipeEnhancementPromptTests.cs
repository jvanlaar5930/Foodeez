using FluentAssertions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.Recipes;
using Xunit;

namespace Foodeez.Application.Tests.Common;

/// <summary>
/// The prompt has to carry the recipe as it actually stands, and the parse has to refuse
/// anything that would be saved over a reader's enhancement as an improvement on it.
/// </summary>
public class RecipeEnhancementPromptTests
{
    private static RecipeDto Recipe() => new()
    {
        Name = "Roast Chicken",
        Description = "Sunday dinner.",
        Instructions = "Season the bird.\nRoast at 200C.",
        PrepTimeMinutes = 15,
        CookTimeMinutes = 90,
        Servings = 4,
        Tags = "dinner,roast",
        Ingredients =
        [
            new RecipeIngredientDto { FoodItemName = "Chicken", Quantity = 1.5f, Unit = "kg" },
            new RecipeIngredientDto { FoodItemName = "Sea salt", Notes = "to taste" }
        ]
    };

    [Fact]
    public void The_prompt_carries_the_dish_its_method_and_its_ingredients()
    {
        var prompt = RecipeEnhancementPrompt.Build(new EnhanceRecipeRequest { Original = Recipe() });

        prompt.Should().Contain("Roast Chicken");
        prompt.Should().Contain("Roast at 200C.");
        prompt.Should().Contain("1.5 kg Chicken");
        prompt.Should().Contain("Serves: 4");
    }

    /// <summary>An amount nobody recorded is left out rather than printed as "0 Sea salt".</summary>
    [Fact]
    public void An_ingredient_with_no_recorded_amount_is_named_without_one()
    {
        var prompt = RecipeEnhancementPrompt.Build(new EnhanceRecipeRequest { Original = Recipe() });

        prompt.Should().Contain("- Sea salt (to taste)");
        prompt.Should().NotContain("0 Sea salt");
    }

    /// <summary>
    /// A recipe cached from a search carries neither. An empty heading reads as "this dish has
    /// no ingredients", which is how a rewrite comes back as a different meal.
    /// </summary>
    [Fact]
    public void A_recipe_with_nothing_stored_says_so_rather_than_showing_empty_headings()
    {
        var thin = new RecipeDto { Name = "Pad Thai" };

        var prompt = RecipeEnhancementPrompt.Build(new EnhanceRecipeRequest { Original = thin });

        prompt.Should().Contain("infer the usual ingredients");
        prompt.Should().Contain("infer the usual method");
    }

    [Fact]
    public void Excluded_foods_are_stated_as_a_hard_constraint()
    {
        var prompt = RecipeEnhancementPrompt.Build(new EnhanceRecipeRequest
        {
            Original = Recipe(),
            ExcludedFoods = ["shellfish"]
        });

        prompt.Should().Contain("shellfish");
        prompt.Should().Contain("never suggest them");
    }

    [Fact]
    public void A_refresh_tells_the_model_what_the_cook_has_already_turned_down()
    {
        var prompt = RecipeEnhancementPrompt.Build(new EnhanceRecipeRequest
        {
            Original = Recipe(),
            PreviousEnhancement = "Spatchcocked over potatoes."
        });

        prompt.Should().Contain("Spatchcocked over potatoes.");
        prompt.Should().Contain("different");
    }

    [Fact]
    public void An_answer_is_read_back_as_a_recipe()
    {
        const string answer = """
            Here you go.
            {
              "name": "Roast Chicken, Elevated",
              "description": "Dry-brined and rested.",
              "instructions": "Dry-brine overnight.\nRoast at 220C.\nRest 20 minutes.",
              "prepTimeMinutes": 20,
              "cookTimeMinutes": 80,
              "servings": 4,
              "tags": ["roast"],
              "ingredients": [
                { "name": "Free-range chicken", "quantity": 1.6, "unit": "kg", "notes": "at room temperature" }
              ],
              "chefNotes": ["Dry-brining seasons the meat and dries the skin, so it crisps."],
              "calories": 540,
              "protein": 46
            }
            """;

        var enhanced = RecipeEnhancementPrompt.Parse(answer);

        enhanced.Should().NotBeNull();
        enhanced!.Instructions.Should().Contain("Dry-brine overnight.");
        enhanced.ChefNotes.Should().ContainSingle().Which.Should().Contain("crisps");
        enhanced.Ingredients.Should().ContainSingle();
        enhanced.Ingredients[0].Notes.Should().Be("at room temperature");
        enhanced.NutritionalInfoPerServing.Calories.Should().Be(540);
        enhanced.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// The method is the whole feature. A rewrite without one, saved over what the reader
    /// already had, leaves them worse off than before they asked.
    /// </summary>
    [Fact]
    public void An_answer_with_no_method_is_nothing_usable()
    {
        RecipeEnhancementPrompt.Parse("""{"name":"Lovely Chicken","chefNotes":["Season it."]}""")
            .Should().BeNull();
    }

    [Fact]
    public void An_answer_that_is_not_json_at_all_is_nothing_usable()
    {
        RecipeEnhancementPrompt.Parse("I would sear it first.").Should().BeNull();
    }

    /// <summary>Nameless ingredients are dropped rather than saved as blank bullet points.</summary>
    [Fact]
    public void Ingredients_with_no_name_are_left_out()
    {
        var enhanced = RecipeEnhancementPrompt.Parse("""
            {
              "instructions": "Cook it.",
              "ingredients": [{ "name": "", "quantity": 2 }, { "name": "Butter", "quantity": 30, "unit": "g" }]
            }
            """);

        enhanced!.Ingredients.Should().ContainSingle().Which.Name.Should().Be("Butter");
    }

    [Fact]
    public void A_provider_that_could_not_answer_reports_it_rather_than_an_empty_recipe()
    {
        RecipeEnhancementPrompt.Unavailable().Succeeded.Should().BeFalse();
    }
}
