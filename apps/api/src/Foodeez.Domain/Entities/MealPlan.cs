using Foodeez.Domain.Common;

namespace Foodeez.Domain.Entities;

public class MealPlan : BaseEntity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsAIGenerated { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MealPlanEntry> Entries { get; set; } = new List<MealPlanEntry>();
}
