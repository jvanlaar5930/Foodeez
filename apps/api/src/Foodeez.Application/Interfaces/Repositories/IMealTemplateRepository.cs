using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IMealTemplateRepository : IBaseRepository<MealTemplate>
{
    /// <summary>A user's saved meals with their items, most-used and most-recent first.</summary>
    Task<IReadOnlyList<MealTemplate>> GetByUserAsync(Guid userId);

    Task<MealTemplate?> GetDetailedByIdAsync(Guid id);

    /// <summary>
    /// The saved meal a name refers to, matched case-insensitively so "Turkey Sandwich" typed
    /// into quick add finds the "turkey sandwich" that was saved.
    /// </summary>
    Task<MealTemplate?> FindByNameAsync(Guid userId, string name);

    /// <summary>
    /// Add an item through its own DbSet. Adding it to a loaded template's collection instead
    /// would have EF treat it as an existing row - BaseEntity hands every new entity an id in
    /// its constructor - and emit an UPDATE that matches nothing.
    /// </summary>
    Task AddItemAsync(MealTemplateItem item);

    void RemoveItem(MealTemplateItem item);
}
