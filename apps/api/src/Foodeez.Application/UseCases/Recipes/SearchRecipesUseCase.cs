using System.Text;
using System.Text.RegularExpressions;
using Foodeez.Application.Common;
using Foodeez.Application.DTOs.MealLogs;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;
using Foodeez.Domain.Entities;
using Foodeez.Domain.ValueObjects;

namespace Foodeez.Application.UseCases.Recipes;

public class SearchRecipesUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpoonacularService _spoonacular;

    private const int DbSufficientThreshold = 3;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(30);

    public SearchRecipesUseCase(IUnitOfWork unitOfWork, ISpoonacularService spoonacular)
    {
        _unitOfWork = unitOfWork;
        _spoonacular = spoonacular;
    }

    public async Task<List<RecipeDto>> ExecuteAsync(string query, CancellationToken ct = default)
    {
        var dbResults = await _unitOfWork.Recipes.SearchAsync(query);

        if (dbResults.Count >= DbSufficientThreshold)
            return dbResults.Select(MapToDto).ToList();

        var spoonacularResults = await _spoonacular.SearchRecipesAsync(query, 10, ct);
        if (spoonacularResults.Count == 0)
            return dbResults.Select(MapToDto).ToList();

        var spoonacularIds = spoonacularResults.Select(r => r.Id).ToList();
        var existingBySpoon = await _unitOfWork.Recipes.GetBySpoonacularIdsAsync(spoonacularIds);
        var existingIdMap = existingBySpoon.ToDictionary(r => r.SpoonacularId!.Value);

        var now = DateTime.UtcNow;
        var toAdd = new List<Recipe>();

        foreach (var sr in spoonacularResults)
        {
            if (existingIdMap.TryGetValue(sr.Id, out var existing))
            {
                if (existing.SpoonacularSyncedAt.HasValue && now - existing.SpoonacularSyncedAt.Value > StaleThreshold)
                    ApplySpoonacularUpdate(existing, sr, now);
                continue;
            }

            toAdd.Add(MapToEntity(sr, now));
        }

        foreach (var recipe in toAdd)
            await _unitOfWork.Recipes.AddAsync(recipe);

        if (toAdd.Count > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        var refreshed = await _unitOfWork.Recipes.SearchAsync(query);
        return refreshed.Select(MapToDto).ToList();
    }

    private static Recipe MapToEntity(SpoonacularRecipeResult sr, DateTime syncedAt)
    {
        var nutrition = ExtractNutrition(sr);

        var recipe = new Recipe
        {
            Name = sr.Title,
            Description = StripHtml(sr.Summary),
            Instructions = BuildInstructions(sr),
            PrepTimeMinutes = sr.PreparationMinutes > 0 ? sr.PreparationMinutes : sr.ReadyInMinutes / 2,
            CookTimeMinutes = sr.CookingMinutes > 0 ? sr.CookingMinutes : sr.ReadyInMinutes / 2,
            Servings = sr.Servings > 0 ? sr.Servings : 1,
            Tags = BuildTags(sr, nutrition),
            ImageUrl = NormalizeImageUrl(sr),
            IsAIGenerated = false,
            SpoonacularId = sr.Id,
            SpoonacularSyncedAt = syncedAt,
            NutritionalInfoPerServing = nutrition,
        };

        foreach (var ing in sr.ExtendedIngredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                IngredientName = ing.Original ?? ing.OriginalName ?? ing.Name,
                Quantity = (float)ing.Amount,
                Unit = ing.Unit,
            });
        }

        return recipe;
    }

    private static void ApplySpoonacularUpdate(Recipe existing, SpoonacularRecipeResult sr, DateTime now)
    {
        var nutrition = ExtractNutrition(sr);
        existing.Name = sr.Title;
        existing.Description = StripHtml(sr.Summary);
        existing.Instructions = BuildInstructions(sr);
        existing.ImageUrl = NormalizeImageUrl(sr) ?? existing.ImageUrl;
        existing.Tags = BuildTags(sr, nutrition);
        existing.NutritionalInfoPerServing = nutrition;
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

        return string.Join(",", tags);
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

    internal static RecipeDto MapToDto(Recipe recipe) => new RecipeDto
    {
        Id = recipe.Id,
        Name = recipe.Name,
        Description = recipe.Description,
        Instructions = recipe.Instructions,
        PrepTimeMinutes = recipe.PrepTimeMinutes,
        CookTimeMinutes = recipe.CookTimeMinutes,
        Servings = recipe.Servings,
        Tags = recipe.Tags,
        ImageUrl = recipe.ImageUrl,
        IsAIGenerated = recipe.IsAIGenerated,
        CreatedByUserId = recipe.CreatedByUserId,
        NutritionalInfoPerServing = new NutritionalInfoDto
        {
            Calories      = recipe.NutritionalInfoPerServing.Calories,
            Protein       = recipe.NutritionalInfoPerServing.Protein,
            Carbohydrates = recipe.NutritionalInfoPerServing.Carbohydrates,
            Fat           = recipe.NutritionalInfoPerServing.Fat,
            Fiber         = recipe.NutritionalInfoPerServing.Fiber,
            Sugar         = recipe.NutritionalInfoPerServing.Sugar,
            Sodium        = recipe.NutritionalInfoPerServing.Sodium
        },
        Ingredients = recipe.Ingredients.Select(i => new RecipeIngredientDto
        {
            FoodItemId   = i.FoodItemId,
            FoodItemName = i.FoodItem?.Name ?? i.IngredientName ?? string.Empty,
            Quantity     = i.Quantity,
            Unit         = i.Unit,
            Notes        = i.Notes
        }).ToList()
    };
}
