# Domain: recipes

## Purpose

Recipe discovery and storage. Search is DB-first with a Spoonacular fallback that caches what
it fetches, so the catalogue fills itself as people use it. Recipe browsing is one of the few
public parts of the app.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/RecipesController.cs` (route `api/recipes`).
- `apps/api/src/Foodeez.Application/UseCases/Recipes/` - `SearchRecipesUseCase`,
  `GetRecipeDetailUseCase`, `AutocompleteRecipesUseCase`, `SavedRecipesUseCase`,
  `RecipeFilterTagsUseCase`, `SpoonacularRecipeMapper`.
- `apps/api/src/Foodeez.Infrastructure/Services/SpoonacularService.cs`,
  `apps/api/src/Foodeez.Infrastructure/Repositories/{RecipeRepository,SavedRecipeRepository}.cs`
- `apps/api/src/Foodeez.Domain/Entities/{Recipe,RecipeIngredient,SavedRecipe}.cs`
- Shared: `packages/shared/src/constants/recipeFilters.ts`,
  `packages/shared/src/types/recipe.ts`.
- Clients: `apps/web/src/views/recipes/RecipesView.vue`,
  `apps/web/src/views/search/SearchResultsView.vue`,
  `apps/web/src/composables/{usePagedRecipes,useRecipeDetail,useRecipeFilterTags}.ts`,
  `apps/mobile/src/screens/recipes/`, `apps/mobile/src/store/savedRecipeStore.ts`.

## Main Flows

**Search** (`GET api/recipes/search`, `[AllowAnonymous]`) - `SearchRecipesUseCase` queries the
local `Recipes` table first, over-fetching one row to learn whether another page exists without
a `COUNT`. A full local page returns immediately; otherwise it calls Spoonacular's
`complexSearch` and caches the results. Paging maps onto Spoonacular's own offset, so page N
costs one upstream call. Default page size 10, max 50, staleness threshold 30 days.

**Detail** (`GET api/recipes/{id:guid}`, `[AllowAnonymous]`) - rows cached from `complexSearch`
are deliberately partial (no ingredients, no instructions - that endpoint returns none).
`GetRecipeDetailUseCase` fills the recipe in lazily the first time someone opens it, which
keeps per-recipe calls off the search path.

**Autocomplete** (`GET api/recipes/autocomplete`, `[AllowAnonymous]`) and the browse list
(`GET api/recipes`, `[AllowAnonymous]`).

**Filter tags** (`GET api/recipes/filter-tags`, `[AllowAnonymous]`) - the filter pills every
client shows. They come from the `recipes.filterTags` row in `AppSettings`, so an administrator
can edit them; a missing or blank row means "never configured" and yields the defaults, while
an explicitly empty array means an administrator switched the pills off. A row that is not JSON
is read as a comma-separated list. The defaults are duplicated in
`DEFAULT_RECIPE_FILTER_TAGS` (`@foodeez/shared`) and **the two lists must stay in step**.

**Saved recipes** (`[Authorize]`) - `GET api/recipes/saved`, `PUT api/recipes/{id}/save`,
`DELETE api/recipes/{id}/save`, handled by `SavedRecipesUseCase`.

## Data and State

`Recipes` + `RecipeIngredients` (cached upstream data plus source attribution added by the
`AddRecipeSourceAttribution` migration), `SavedRecipes` (user-to-recipe join with `SavedAt`).
Recipes are shared across users; only the save is per-user.

## External Systems

Spoonacular via `ISpoonacularService`. Its typed `HttpClient` **does** carry
`AddStandardResilienceHandler()` - these are idempotent GETs, so a 503 is worth retrying rather
than surfacing as an empty search. `Spoonacular:ApiKey` is optional; without it, search is
limited to whatever is already cached locally.

## Tests and Validation

`apps/api/tests/Foodeez.Application.Tests/UseCases/RecipeFilterTagsUseCaseTests.cs`.
`Needs verification` - `SearchRecipesUseCase` and `GetRecipeDetailUseCase` have no unit tests.

## Known Gotchas

- Most recipe endpoints are `[AllowAnonymous]` on an otherwise `[Authorize]` controller. Use
  `UserIdOrNull`, never `UserId`, in those actions - `UserId` throws for an anonymous caller.
- A cached recipe may be a partial row. Do not assume ingredients or instructions are present
  without going through `GetRecipeDetailUseCase`.
- Changing the default filter tags means changing both `RecipeFilterTagsUseCase.Defaults` and
  `DEFAULT_RECIPE_FILTER_TAGS`; the comments in each file say so.
- The 30-day staleness threshold is duplicated in `SearchRecipesUseCase` and
  `SearchFoodItemsUseCase` - they are separate constants that happen to agree.
