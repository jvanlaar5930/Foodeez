using System.Text;
using System.Text.RegularExpressions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// Translates Spoonacular payloads into <see cref="Recipe"/> entities and DTOs.
/// Shared by search (which caches lightweight results) and detail hydration (which fills in
/// the ingredients and instructions that the search endpoint does not return).
/// </summary>
internal static class SpoonacularRecipeMapper
{
    // Column widths from RecipeConfiguration. An oversized value fails the whole
    // SaveChanges - and with it the search - so every mapped string is clamped.
    private const int NameMaxLength = 200;

    // Description is a MySQL TEXT column: 65,535 *bytes*, which in utf8mb4 guarantees only
    // 16,383 characters. This is a backstop against a pathological payload, not a display
    // limit - real summaries land well under it and arrive whole.
    private const int DescriptionMaxLength = 16_000;
    private const int TagsMaxLength = 500;
    private const int ImageUrlMaxLength = 500;
    private const int UnitMaxLength = 50;
    private const int SourceUrlMaxLength = 500;
    private const int SourceNameMaxLength = 200;

    /// <summary>
    /// True when a cached row is missing the parts `complexSearch` never returns and we have
    /// not already asked for them. The timestamp check matters: some recipes have no method
    /// upstream either, and re-checking those on every view would burn a call each time.
    /// </summary>
    public static bool NeedsDetail(Recipe recipe) =>
        (recipe.DetailFetchedAt is null &&
         (string.IsNullOrWhiteSpace(recipe.Instructions) || recipe.Ingredients.Count == 0)) ||
        WasTruncated(recipe.Description);

    /// <summary>
    /// Rows cached while Description was a varchar(1000) kept the clipped text after the
    /// column was widened, and no amount of re-reading the database will bring the rest back.
    /// The ellipsis this mapper appends is the marker that says so, letting each stale row
    /// repair itself on its next detail fetch. Self-limiting: the refreshed value has no
    /// marker, so it never asks twice.
    /// </summary>
    private static bool WasTruncated(string? value) =>
        value is not null && value.AsSpan().TrimEnd().EndsWith("…");

    public static Recipe MapToEntity(SpoonacularRecipeResult sr, DateTime syncedAt)
    {
        var nutrition = ExtractNutrition(sr);

        var recipe = new Recipe
        {
            Name = Truncate(sr.Title, NameMaxLength) ?? string.Empty,
            Description = Truncate(StripHtml(sr.Summary), DescriptionMaxLength),
            Instructions = BuildInstructions(sr),
            PrepTimeMinutes = sr.PreparationMinutes > 0 ? sr.PreparationMinutes : sr.ReadyInMinutes / 2,
            CookTimeMinutes = sr.CookingMinutes > 0 ? sr.CookingMinutes : sr.ReadyInMinutes / 2,
            Servings = sr.Servings > 0 ? sr.Servings : 1,
            Tags = BuildTags(sr, nutrition),
            ImageUrl = Truncate(NormalizeImageUrl(sr), ImageUrlMaxLength),
            IsAIGenerated = false,
            SpoonacularId = sr.Id,
            SpoonacularSyncedAt = syncedAt,
            SourceUrl = Truncate(sr.SourceUrl, SourceUrlMaxLength),
            SourceName = Truncate(sr.SourceName ?? sr.CreditsText, SourceNameMaxLength),
            NutritionalInfoPerServing = nutrition,
        };

        foreach (var ingredient in BuildIngredients(sr))
            recipe.Ingredients.Add(ingredient);

        return recipe;
    }

    /// <summary>
    /// Builds the ingredient list, preferring the rich `extendedIngredients` that only the
    /// detail endpoint returns and falling back to the slimmer `nutrition.ingredients` that
    /// rides along with a search response. Pure: attaching these to a tracked recipe is the
    /// repository's job, since the change tracker needs them marked as inserts.
    /// </summary>
    public static List<RecipeIngredient> BuildIngredients(SpoonacularRecipeResult sr)
    {
        if (sr.ExtendedIngredients.Count > 0)
        {
            return sr.ExtendedIngredients
                .Select(ing => new RecipeIngredient
                {
                    IngredientName = ing.Original ?? ing.OriginalName ?? ing.Name,
                    Quantity = (float)ing.Amount,
                    Unit = Truncate(ing.Unit, UnitMaxLength) ?? string.Empty,
                })
                .ToList();
        }

        return (sr.Nutrition?.Ingredients ?? [])
            .Select(ing => new RecipeIngredient
            {
                IngredientName = ing.Name,
                Quantity = (float)ing.Amount,
                Unit = Truncate(ing.Unit, UnitMaxLength) ?? string.Empty,
            })
            .ToList();
    }

    /// <summary>
    /// Applies a full `/recipes/{id}/information` payload onto a cached row, filling the
    /// ingredients and instructions that search could never provide. Fields the detail call
    /// leaves blank keep whatever the search response already stored.
    /// </summary>
    public static void ApplyDetail(Recipe recipe, SpoonacularRecipeResult sr, DateTime now)
    {
        var instructions = BuildInstructions(sr);
        if (!string.IsNullOrWhiteSpace(instructions))
            recipe.Instructions = instructions;

        if (!string.IsNullOrWhiteSpace(sr.Title))
            recipe.Name = Truncate(sr.Title, NameMaxLength) ?? recipe.Name;

        var description = Truncate(StripHtml(sr.Summary), DescriptionMaxLength);
        if (!string.IsNullOrWhiteSpace(description))
            recipe.Description = description;

        recipe.ImageUrl = Truncate(NormalizeImageUrl(sr), ImageUrlMaxLength) ?? recipe.ImageUrl;

        if (sr.Servings > 0) recipe.Servings = sr.Servings;
        if (sr.PreparationMinutes > 0) recipe.PrepTimeMinutes = sr.PreparationMinutes;
        if (sr.CookingMinutes > 0) recipe.CookTimeMinutes = sr.CookingMinutes;

        // The detail call carries nutrition only when asked for it; don't wipe good numbers.
        if (sr.Nutrition is { Nutrients.Count: > 0 })
        {
            var nutrition = ExtractNutrition(sr);
            recipe.NutritionalInfoPerServing = nutrition;
            recipe.Tags = BuildTags(sr, nutrition);
        }

        recipe.SourceUrl = Truncate(sr.SourceUrl, SourceUrlMaxLength) ?? recipe.SourceUrl;
        recipe.SourceName = Truncate(sr.SourceName ?? sr.CreditsText, SourceNameMaxLength) ?? recipe.SourceName;
        recipe.SpoonacularSyncedAt = now;
        recipe.DetailFetchedAt = now;
    }

    public static void ApplySpoonacularUpdate(Recipe existing, SpoonacularRecipeResult sr, DateTime now)
    {
        var nutrition = ExtractNutrition(sr);
        existing.Name = Truncate(sr.Title, NameMaxLength) ?? existing.Name;
        existing.Description = Truncate(StripHtml(sr.Summary), DescriptionMaxLength);
        existing.Instructions = BuildInstructions(sr);
        existing.ImageUrl = Truncate(NormalizeImageUrl(sr), ImageUrlMaxLength) ?? existing.ImageUrl;
        existing.Tags = BuildTags(sr, nutrition);
        existing.NutritionalInfoPerServing = nutrition;
        existing.SourceUrl = Truncate(sr.SourceUrl, SourceUrlMaxLength) ?? existing.SourceUrl;
        existing.SourceName = Truncate(sr.SourceName ?? sr.CreditsText, SourceNameMaxLength) ?? existing.SourceName;
        existing.SpoonacularSyncedAt = now;
    }

    // ── Nutrition ────────────────────────────────────────────────────────────

    private static NutritionalInfo ExtractNutrition(SpoonacularRecipeResult sr)
    {
        if (sr.Nutrition == null)
            return NutritionalInfo.Empty;

        float Get(params string[] names)
        {
            foreach (var name in names)
            {
                var n = sr.Nutrition.Nutrients
                    .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (n != null) return (float)n.Amount;
            }
            return 0f;
        }

        return new NutritionalInfo(
            calories:      Get("Calories"),
            protein:       Get("Protein"),
            carbohydrates: Get("Carbohydrates", "Net Carbohydrates"),
            fat:           Get("Fat", "Total Fat"),
            fiber:         Get("Fiber", "Dietary Fiber"),
            sugar:         Get("Sugar", "Total Sugars"),
            sodium:        Get("Sodium")
        );
    }

    // ── Instructions ─────────────────────────────────────────────────────────

    private static string BuildInstructions(SpoonacularRecipeResult sr)
    {
        // Prefer structured steps — they're cleaner and already stripped of HTML
        var groups = sr.AnalyzedInstructions
            .Where(g => g.Steps.Count > 0)
            .ToList();

        if (groups.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var group in groups)
            {
                if (!string.IsNullOrWhiteSpace(group.Name))
                    sb.AppendLine(group.Name);

                foreach (var step in group.Steps.OrderBy(s => s.Number))
                    sb.AppendLine($"{step.Number}. {step.Step.Trim()}");
            }
            return sb.ToString().Trim();
        }

        // Fall back to plain instructions text (may contain HTML)
        return StripHtml(sr.Instructions) ?? string.Empty;
    }

    // ── Tags ─────────────────────────────────────────────────────────────────

    private static readonly HashSet<string> DishTypeTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        "breakfast", "brunch", "lunch", "dinner", "snack", "appetizer", "dessert",
        "side dish", "salad", "soup", "beverage", "sauce", "bread"
    };

    private static string BuildTags(SpoonacularRecipeResult sr, NutritionalInfo nutrition)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Dietary flags
        if (sr.Vegetarian) tags.Add("Vegetarian");
        if (sr.Vegan) tags.Add("Vegan");
        if (sr.GlutenFree) tags.Add("Gluten-Free");
        if (sr.DairyFree) tags.Add("Dairy-Free");
        if (sr.LowFodmap) tags.Add("Low-FODMAP");
        if (sr.VeryHealthy) tags.Add("Healthy");

        // Diet labels from Spoonacular's diets array
        foreach (var diet in sr.Diets)
        {
            var normalized = diet.ToLower() switch
            {
                "low carb" or "low carb diet" => "Low-Carb",
                "ketogenic" => "Keto",
                "paleolithic" or "paleo" or "primal" => "Paleo",
                "whole 30" or "whole30" => "Whole30",
                "gluten free" => "Gluten-Free",
                "dairy free" => "Dairy-Free",
                "lacto ovo vegetarian" or "vegetarian" or "ovo vegetarian" or "lacto vegetarian" => "Vegetarian",
                "vegan" => "Vegan",
                "pescatarian" => "Pescatarian",
                _ => null
            };
            if (normalized != null) tags.Add(normalized);
        }

        // Quick: ready in 30 min or less
        if (sr.ReadyInMinutes > 0 && sr.ReadyInMinutes <= 30) tags.Add("Quick");

        // High-protein: ≥25g protein per serving
        if (nutrition.Protein >= 25f) tags.Add("High-Protein");

        // Cuisine types (title-cased, cap at 3)
        foreach (var cuisine in sr.Cuisines.Take(3))
            tags.Add(ToTitleCase(cuisine));

        // Dish types that are meaningful meal-prep context
        foreach (var dish in sr.DishTypes)
        {
            if (DishTypeTagMap.Contains(dish))
                tags.Add(ToTitleCase(dish));
        }

        // Drop whole tags rather than truncating mid-word, which would corrupt the last one.
        var joined = string.Join(",", tags);
        while (joined.Length > TagsMaxLength && tags.Count > 0)
        {
            tags.Remove(tags.Last());
            joined = string.Join(",", tags);
        }

        return joined;
    }

    // ── Image ────────────────────────────────────────────────────────────────

    private static string? NormalizeImageUrl(SpoonacularRecipeResult sr)
    {
        if (string.IsNullOrWhiteSpace(sr.Image)) return null;

        // complexSearch returns thumbnails like "…-312x231.jpg" — upgrade to a larger size
        var url = sr.Image;
        if (url.Contains("-312x231"))
            url = url.Replace("-312x231", "-636x393");
        else if (url.Contains("-240x150"))
            url = url.Replace("-240x150", "-636x393");

        return url;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static readonly Regex HtmlRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultiSpaceRegex = new(@"\s{2,}", RegexOptions.Compiled);

    /// <summary>Clamp a value to its column width, trimming at a word boundary where one is close.</summary>
    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;

        // Leave room for the ellipsis so the result still fits the column exactly.
        var cut = value[..(maxLength - 1)];
        var lastSpace = cut.LastIndexOf(' ');
        if (lastSpace > maxLength - 40) cut = cut[..lastSpace];
        return cut.TrimEnd() + "…";
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = HtmlRegex.Replace(html, " ");
        text = MultiSpaceRegex.Replace(text, " ").Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string ToTitleCase(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    /// <summary>Mapping a stored recipe is not this class of thing's job; it forwards so
    /// existing call sites keep working. <see cref="RecipeMapper"/> owns the shape.</summary>
    public static RecipeDto MapToDto(Recipe recipe) => RecipeMapper.ToDto(recipe);
}
