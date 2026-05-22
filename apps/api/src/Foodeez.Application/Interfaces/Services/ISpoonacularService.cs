namespace Foodeez.Application.Interfaces.Services;

public interface ISpoonacularService
{
    Task<IReadOnlyList<SpoonacularRecipeResult>> SearchRecipesAsync(string query, int number = 10, CancellationToken ct = default);
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
    public SpoonacularCaloricBreakdown? CaloricBreakdown { get; set; }
    public SpoonacularWeightPerServing? WeightPerServing { get; set; }
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
