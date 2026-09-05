# Domain: shared-package

## Purpose

`packages/shared` (`@foodeez/shared`) - the one definition of the models, enums, constants and
nutrition maths that both clients use, so the web app and the mobile app cannot drift from each
other or from the API's JSON.

## Key Files

- `packages/shared/src/index.ts` - re-exports `enums`, `types`, `constants`, `utils`.
- `src/enums.ts` - `Gender`, `ActivityLevel`, `DietaryGoal`, `MealType`, `UnitSystem`,
  and the chat role. String enums whose values are the **C# member names**.
- `src/types/` - `api`, `user`, `meal`, `nutrition`, `recipe`, `mealPlan`, `chat`, `grocery`.
- `src/constants/` - `nutrition`, `aiRecipe`, `recipeFilters`
  (`DEFAULT_RECIPE_FILTER_TAGS`).
- `src/utils/` - `nutrition`, `units`, `analysisScore`.
- `packages/shared/tests/{nutrition,units}.test.ts`, `packages/shared/tsconfig.json`.

## Main Flows

Plain TypeScript compiled by `tsc` to `dist/` with declarations (`npm run shared:build`, or
`npm run shared:dev` to watch). Consumers:

- **Web** - Vite and Vitest alias `@foodeez/shared` directly to `src/index.ts`, so changes are
  live without a build.
- **Mobile** - Jest maps it to `src/index.ts`; the app itself resolves the package's `dist`
  through the workspace, so a build is needed after changing shared code.
- **Docker** - `apps/web/Dockerfile` runs `npm run shared:build` before building the web app.

## Data and State

Stateless. Types only, plus pure functions for nutrition totals, unit conversion (US vs.
metric, per `UnitSystem`) and the meal analysis score bands.

## External Systems

None. It has no runtime dependencies - `typescript` and `vitest` are its only devDependencies.

## Tests and Validation

Vitest: `packages/shared/tests/nutrition.test.ts` and `units.test.ts`. `npm run shared:test`.

## Known Gotchas

- **Enum values must match the C# member names exactly.** The API serialises enums as strings
  (`JsonStringEnumConverter`), so renaming a member on either side breaks both clients silently
  at runtime; the files say so in comments.
- `DEFAULT_RECIPE_FILTER_TAGS` duplicates `RecipeFilterTagsUseCase.Defaults` on the API by
  design (it is the pre-response fallback) - change them together.
- The mobile app re-exports these through `apps/mobile/src/types/index.ts` and sometimes names a
  type `...Dto` locally where the shared package does not - check both before adding a new one.
- A type that only exists in `src/` and not in a fresh `dist/` will pass web tests and fail the
  Docker build; run `npm run shared:build` before trusting a green run.
