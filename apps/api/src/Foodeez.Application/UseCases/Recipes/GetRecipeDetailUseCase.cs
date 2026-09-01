using Foodeez.Application.Common;
using Foodeez.Application.DTOs.Recipes;
using Foodeez.Application.Interfaces.Services;

namespace Foodeez.Application.UseCases.Recipes;

/// <summary>
/// Returns one recipe, filling in the parts a search response never carries.
///
/// Spoonacular's `complexSearch` returns no ingredients and no instructions at all, so a row
/// cached from a search is only good enough for a result card. The first time someone opens
/// that recipe we spend one detail call to complete it and write the result back, so the cost
/// falls only on recipes people actually read and is paid once each.
/// </summary>
public class GetRecipeDetailUseCase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISpoonacularService _spoonacular;

    public GetRecipeDetailUseCase(IUnitOfWork unitOfWork, ISpoonacularService spoonacular)
    {
        _unitOfWork = unitOfWork;
        _spoonacular = spoonacular;
    }

    public async Task<RecipeDto?> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var recipe = await _unitOfWork.Recipes.GetByIdAsync(id);
        if (recipe == null) return null;

        var detailUnavailable = false;

        if (recipe.SpoonacularId.HasValue && SpoonacularRecipeMapper.NeedsDetail(recipe))
        {
            var detail = await _spoonacular.GetRecipeInformationAsync(recipe.SpoonacularId.Value, ct);
            if (detail != null)
            {
                SpoonacularRecipeMapper.ApplyDetail(recipe, detail, DateTime.UtcNow);

                var ingredients = SpoonacularRecipeMapper.BuildIngredients(detail);
                if (ingredients.Count > 0)
                    _unitOfWork.Recipes.ReplaceIngredients(recipe, ingredients);

                await _unitOfWork.SaveChangesAsync(ct);
            }
            else
            {
                // We needed the method and the request came back empty. DetailFetchedAt is
                // deliberately left unset so the next read tries again once the upstream
                // recovers - but this reader deserves to know why the page is thin.
                detailUnavailable = true;
            }
        }

        var dto = SpoonacularRecipeMapper.MapToDto(recipe);
        dto.DetailUnavailable = detailUnavailable;
        return dto;
    }
}
