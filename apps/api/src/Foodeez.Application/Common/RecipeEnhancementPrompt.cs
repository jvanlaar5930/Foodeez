using System.Text;
using System.Text.Json;
using Foodeez.Application.DTOs.AI;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;

namespace Foodeez.Application.Common;

/// <summary>
/// The prompt and response parsing for raising a recipe to restaurant standard.
///
/// The instruction that matters most here is that this is the *same dish*. Asked simply to
/// "improve" a recipe, a model will happily return a different meal - the roast chicken comes
/// back as a confit, the weeknight pasta as a five-hour ragu - and the reader who wanted their
/// recipe made better has instead lost it. So the dish, and roughly the effort it takes, are
/// fixed, and technique is what is allowed to change.
/// </summary>
public static class RecipeEnhancementPrompt
{
    public static string Build(EnhanceRecipeRequest request)
    {
        var recipe = request.Original;
        var sb = new StringBuilder();

        sb.AppendLine(
            "You are a Michelin-starred chef. Take the home recipe below and rewrite it the way " +
            "you would cook it in your own kitchen: the same dish, brought to its best.");
        sb.AppendLine();
        sb.AppendLine("What that means here:");
        sb.AppendLine("- Keep the dish recognisably itself. Same core ingredients, same meal, same course.");
        sb.AppendLine("- Improve it through technique, seasoning, timing and order of work - searing before " +
                      "braising, salting ahead, resting, blooming spices, finishing with acid or fat.");
        sb.AppendLine("- Add or adjust ingredients only where they genuinely lift the dish, and prefer " +
                      "everyday ones. Aromatics, herbs, acid, stock and good fat earn their place; " +
                      "truffle and gold leaf do not.");
        sb.AppendLine("- Keep it cookable in a home kitchen with ordinary equipment.");
        sb.AppendLine("- Stay close to the original effort. Another half hour is fine; turning a " +
                      "30-minute supper into a two-day project is not.");
        sb.AppendLine("- Write the method as steps in order, one per line, each precise about heat, " +
                      "time and what to look for.");
        sb.AppendLine();

        AppendOriginal(sb, recipe);

        if (request.ExcludedFoods.Count > 0)
        {
            sb.AppendLine(AINarration.ExclusionLine(request.ExcludedFoods));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(request.PreviousEnhancement))
        {
            sb.AppendLine("You have enhanced this recipe once already, as follows, and the cook asked for a " +
                          "different take. Go a genuinely different way this time - another technique, " +
                          "another seasoning, another flavour direction - rather than rewording this:");
            sb.AppendLine(request.PreviousEnhancement.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("Estimate the per-serving nutrition of your version, not the original's.");
        sb.AppendLine();
        sb.AppendLine("In \"chefNotes\", give 3-5 short notes saying what you changed and why it makes the " +
                      "dish better. These are the advice the cook keeps, so make each one specific to this " +
                      "recipe - \"salt the aubergine 20 minutes ahead so it draws water and browns instead " +
                      "of steaming\", not \"season well\".");
        sb.AppendLine();
        sb.AppendLine("Respond with JSON only, no prose and no code fences:");
        sb.AppendLine(ResponseShape);

        return sb.ToString();
    }

    /// <summary>
    /// The answer's shape, by example. A verbatim literal rather than an interpolated one so
    /// the JSON braces stay braces - and so the escaped newline in "instructions" reaches the
    /// model as the two characters it must actually write.
    /// </summary>
    private const string ResponseShape = """
        {
          "name": "the dish, named as you would put it on a menu",
          "description": "one or two sentences on what makes this version sing",
          "instructions": "Step one.\nStep two.\nStep three.",
          "prepTimeMinutes": 0,
          "cookTimeMinutes": 0,
          "servings": 0,
          "tags": ["tag"],
          "ingredients": [
            { "name": "ingredient", "quantity": 0, "unit": "g", "notes": "finely diced" }
          ],
          "chefNotes": ["what changed, and why it is better"],
          "calories": 0,
          "protein": 0,
          "carbohydrates": 0,
          "fat": 0,
          "fiber": 0,
          "sugar": 0,
          "sodium": 0
        }
        """;

    private static void AppendOriginal(StringBuilder sb, RecipeDto recipe)
    {
        sb.AppendLine($"Dish: {recipe.Name}");

        if (!string.IsNullOrWhiteSpace(recipe.Description))
        {
            sb.AppendLine($"Description: {recipe.Description.Trim()}");
        }

        sb.AppendLine($"Serves: {(recipe.Servings > 0 ? recipe.Servings : 1)}");
        sb.AppendLine($"Prep: {recipe.PrepTimeMinutes} min, Cook: {recipe.CookTimeMinutes} min");

        if (!string.IsNullOrWhiteSpace(recipe.Tags))
        {
            sb.AppendLine($"Tags: {recipe.Tags}");
        }

        sb.AppendLine();
        sb.AppendLine("Ingredients:");

        if (recipe.Ingredients.Count > 0)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                sb.AppendLine($"- {Describe(ingredient)}");
            }
        }
        else
        {
            // Nothing is stored for this one - a search-only row, or a source that publishes
            // none. Saying so beats an empty heading, which a model reads as "this dish has no
            // ingredients" and answers with something else entirely.
            sb.AppendLine("- (not recorded - infer the usual ingredients for a dish of this name)");
        }

        sb.AppendLine();
        sb.AppendLine("Method:");
        sb.AppendLine(string.IsNullOrWhiteSpace(recipe.Instructions)
            ? "(not recorded - infer the usual method for a dish of this name)"
            : recipe.Instructions.Trim());
        sb.AppendLine();
    }

    /// <summary>One ingredient line, leaving out an amount nobody recorded rather than "0".</summary>
    private static string Describe(RecipeIngredientDto ingredient)
    {
        var amount = ingredient.Quantity > 0
            ? $"{ingredient.Quantity:0.##} {ingredient.Unit}".Trim() + " "
            : string.Empty;

        var notes = string.IsNullOrWhiteSpace(ingredient.Notes) ? string.Empty : $" ({ingredient.Notes.Trim()})";

        return $"{amount}{ingredient.FoodItemName}{notes}";
    }

    /// <summary>
    /// The enhanced recipe in a model's answer, or null when there is nothing usable in it.
    ///
    /// An answer with no method counts as nothing usable. The method is the whole point of the
    /// feature, and a named recipe with no steps written over someone's existing enhancement
    /// would leave them worse off than before they asked.
    /// </summary>
    public static EnhancedRecipeDto? Parse(string responseText)
    {
        var root = JsonExtraction.ReadObject(responseText);
        if (root == null)
        {
            return null;
        }

        var element = root.Value;

        var instructions = JsonExtraction.ReadString(element, "instructions");
        if (instructions.Length == 0)
        {
            return null;
        }

        var description = JsonExtraction.ReadString(element, "description");

        var enhanced = new EnhancedRecipeDto
        {
            Name = JsonExtraction.ReadString(element, "name"),
            Description = description.Length > 0 ? description : null,
            Instructions = instructions,
            PrepTimeMinutes = JsonExtraction.ReadInt(element, "prepTimeMinutes"),
            CookTimeMinutes = JsonExtraction.ReadInt(element, "cookTimeMinutes"),
            Servings = JsonExtraction.ReadInt(element, "servings", 1),
            Tags = JsonExtraction.ReadStrings(element, "tags"),
            ChefNotes = JsonExtraction.ReadStrings(element, "chefNotes"),
            NutritionalInfoPerServing = new NutritionalInfoDto
            {
                Calories = JsonExtraction.ReadFloat(element, "calories"),
                Protein = JsonExtraction.ReadFloat(element, "protein"),
                Carbohydrates = JsonExtraction.ReadFloat(element, "carbohydrates"),
                Fat = JsonExtraction.ReadFloat(element, "fat"),
                Fiber = JsonExtraction.ReadFloat(element, "fiber"),
                Sugar = JsonExtraction.ReadFloat(element, "sugar"),
                Sodium = JsonExtraction.ReadFloat(element, "sodium")
            }
        };

        if (element.TryGetProperty("ingredients", out var ingredients) && ingredients.ValueKind == JsonValueKind.Array)
        {
            foreach (var ingredient in ingredients.EnumerateArray())
            {
                var name = JsonExtraction.ReadString(ingredient, "name");
                if (name.Length == 0)
                {
                    continue;
                }

                var notes = JsonExtraction.ReadString(ingredient, "notes");

                enhanced.Ingredients.Add(new EnhancedIngredientDto
                {
                    Name = name,
                    Quantity = JsonExtraction.ReadFloat(ingredient, "quantity"),
                    Unit = JsonExtraction.ReadString(ingredient, "unit"),
                    Notes = notes.Length > 0 ? notes : null
                });
            }
        }

        return enhanced;
    }

    /// <summary>What a provider that could not answer comes back with. Never saved.</summary>
    public static EnhancedRecipeDto Unavailable() => new() { Succeeded = false };
}
