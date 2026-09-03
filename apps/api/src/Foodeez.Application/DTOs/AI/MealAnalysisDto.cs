namespace Foodeez.Application.DTOs.AI;

public class MealAnalysisRequest
{
    public string MealType { get; set; } = string.Empty;
    public List<MealAnalysisItemRequest> Items { get; set; } = new();

    /// <summary>Whose meal this is, so their exclusions can be looked up. Optional.</summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Foods this person cannot or will not eat. Nothing suggested may include them, so they
    /// are filled in from the profile server-side rather than trusted from the caller.
    /// </summary>
    public List<string> ExcludedFoods { get; set; } = new();
}

public class MealAnalysisItemRequest
{
    public string Name { get; set; } = string.Empty;
    public float Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public float Calories { get; set; }
    public float Protein { get; set; }
    public float Carbs { get; set; }
    public float Fat { get; set; }
    public float Fiber { get; set; }
}

public class MealAnalysisDto
{
    public int Score { get; set; }
    public string Completeness { get; set; } = string.Empty;
    public List<string> Missing { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();

    /// <summary>When the model produced this analysis; null for one that has never been stored.</summary>
    public DateTime? GeneratedAt { get; set; }
}
