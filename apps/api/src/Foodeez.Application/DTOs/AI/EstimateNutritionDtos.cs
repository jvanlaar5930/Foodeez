using Foodeez.Application.DTOs.MealLogs;

namespace Foodeez.Application.DTOs.AI;

/// <summary>
/// Everything a nutritionist would need to ballpark a home-cooked dish. Ingredients are free
/// text on purpose - people describe what they cooked, they do not fill in a form.
/// </summary>
public class EstimateNutritionRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Ingredients and rough amounts, one per line. Optional but hugely improves accuracy.</summary>
    public string? Ingredients { get; set; }

    /// <summary>How many servings the whole batch makes.</summary>
    public int Servings { get; set; } = 1;

    /// <summary>How the user measures one serving, e.g. "1 slice" or "250 g".</summary>
    public string? ServingDescription { get; set; }
}

public class EstimatedNutritionDto
{
    public NutritionalInfoDto PerServing { get; set; } = new();

    /// <summary>The numeric serving size the estimate is based on, for the food item record.</summary>
    public float ServingSize { get; set; } = 1;
    public string ServingUnit { get; set; } = "serving";

    /// <summary>low | medium | high - lower when the description was vague.</summary>
    public string Confidence { get; set; } = "low";

    /// <summary>What the model had to assume, so the user can judge and correct the numbers.</summary>
    public string? Assumptions { get; set; }

    /// <summary>The ingredient list it actually costed, which may be inferred from the dish name.</summary>
    public List<string> AssumedIngredients { get; set; } = new();

    /// <summary>False when the estimate could not be produced; the caller should let the user type values.</summary>
    public bool Succeeded { get; set; } = true;
}
