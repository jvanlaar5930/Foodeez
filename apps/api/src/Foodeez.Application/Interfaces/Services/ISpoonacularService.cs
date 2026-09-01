namespace Foodeez.Application.Interfaces.Services;

public interface ISpoonacularService
{
    Task<IReadOnlyList<SpoonacularRecipeResult>> SearchRecipesAsync(string query, int number = 10, CancellationToken ct = default);

    /// <summary>
    /// A paged search. Uses the upstream's own offset so page N costs one call rather than
    /// refetching everything before it.
    /// </summary>
    Task<SpoonacularSearchPage> SearchRecipesPagedAsync(string query, int offset, int number, CancellationToken ct = default);

    /// <summary>Lightweight title-only suggestions for search-as-you-type.</summary>
    Task<IReadOnlyList<SpoonacularAutocompleteResult>> AutocompleteAsync(string query, int number = 8, CancellationToken ct = default);

    /// <summary>
    /// Full information for one recipe. This is the only endpoint that returns
    /// <c>extendedIngredients</c> and <c>analyzedInstructions</c> - a search response carries
    /// neither - so it has to be called before a recipe can actually be cooked from.
    /// </summary>
    Task<SpoonacularRecipeResult?> GetRecipeInformationAsync(int spoonacularId, CancellationToken ct = default);
}

public class SpoonacularSearchPage
{
    public List<SpoonacularRecipeResult> Results { get; set; } = new();

    /// <summary>How many matches exist upstream in total, for the client's "has more" check.</summary>
    public int TotalResults { get; set; }
}

public class SpoonacularAutocompleteResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageType { get; set; }
}

public class SpoonacularRecipeResult
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int ReadyInMinutes { get; set; }
    public int PreparationMinutes { get; set; }
    public int CookingMinutes { get; set; }
    public int Servings { get; set; }
    public string? Image { get; set; }
    public string? Instructions { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceName { get; set; }
    public string? CreditsText { get; set; }

    // Dietary flags
    public bool Vegetarian { get; set; }
    public bool Vegan { get; set; }
    public bool GlutenFree { get; set; }
    public bool DairyFree { get; set; }
    public bool VeryHealthy { get; set; }
    public bool Cheap { get; set; }
    public bool VeryPopular { get; set; }
    public bool LowFodmap { get; set; }
    public bool Sustainable { get; set; }

    // Classification arrays
    public List<string> Cuisines { get; set; } = new();
    public List<string> DishTypes { get; set; } = new();
    public List<string> Diets { get; set; } = new();
    public List<string> Occasions { get; set; } = new();

    // Scoring
    public double HealthScore { get; set; }
    public double SpoonacularScore { get; set; }
    public double PricePerServing { get; set; }
    public int AggregateLikes { get; set; }

    // Structured instructions (preferred over plain Instructions)
    public List<SpoonacularInstructionGroup> AnalyzedInstructions { get; set; } = new();

    public List<SpoonacularIngredient> ExtendedIngredients { get; set; } = new();
    public SpoonacularNutrition? Nutrition { get; set; }
}

public class SpoonacularInstructionGroup
{
    public string Name { get; set; } = string.Empty;
    public List<SpoonacularStep> Steps { get; set; } = new();
}

public class SpoonacularStep
{
    public int Number { get; set; }
    public string Step { get; set; } = string.Empty;
}

public class SpoonacularIngredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string? Original { get; set; }
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string? Aisle { get; set; }
    public string? Image { get; set; }
}

public class SpoonacularNutrition
{
    public List<SpoonacularNutrient> Nutrients { get; set; } = new();

    /// <summary>
    /// A slimmer ingredient list (name/amount/unit only) that rides along with a search
    /// response when nutrition is requested. It is the only ingredient data search gives us.
    /// </summary>
    public List<SpoonacularNutritionIngredient> Ingredients { get; set; } = new();

    public SpoonacularCaloricBreakdown? CaloricBreakdown { get; set; }
    public SpoonacularWeightPerServing? WeightPerServing { get; set; }
}

public class SpoonacularNutritionIngredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class SpoonacularNutrient
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public double PercentOfDailyNeeds { get; set; }
}

public class SpoonacularCaloricBreakdown
{
    public double PercentProtein { get; set; }
    public double PercentFat { get; set; }
    public double PercentCarbs { get; set; }
}

public class SpoonacularWeightPerServing
{
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
}
