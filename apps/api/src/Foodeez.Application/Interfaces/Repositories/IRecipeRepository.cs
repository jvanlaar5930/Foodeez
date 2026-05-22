using Foodeez.Domain.Entities;

namespace Foodeez.Application.Interfaces.Repositories;

public interface IRecipeRepository : IBaseRepository<Recipe>
{
    Task<IReadOnlyList<Recipe>> SearchAsync(string query);
    Task<IReadOnlyList<Recipe>> GetByTagsAsync(IEnumerable<string> tags);
    Task<IReadOnlyList<Recipe>> GetBySpoonacularIdsAsync(IEnumerable<int> spoonacularIds);
}
