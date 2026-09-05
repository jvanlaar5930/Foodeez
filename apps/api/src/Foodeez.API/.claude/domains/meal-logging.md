# Domain: meal-logging

## Purpose

The core of the app: recording what someone ate, totalling its nutrition, and layering AI
analysis on top - per meal and per day. Includes reusable meal templates and the food-item
catalogue that meal items point at.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/MealLogsController.cs` (route `api/meal-logs`),
  `MealTemplatesController.cs` (`api/meal-templates`), `FoodItemsController.cs` (`api/food-items`).
- `apps/api/src/Foodeez.Application/UseCases/MealLogs/` - 16 files, notably
  `LogMealUseCase`, `UpdateMealLogUseCase`, `DeleteMealLogUseCase`, `QuickAddMealUseCase`,
  `ParseMealImageUseCase`, `GetDailyLogsUseCase`, `GetMealLogsRangeUseCase`,
  `GetNutritionSummaryUseCase`, `GetNutritionReportUseCase`, `AnalyzeMealLogUseCase`,
  `AnalyzeDayUseCase`, plus `MealLogRequestApplier`, `ParsedMealResolver`, `MealLogMapper`,
  `MealAnalysisMapper`.
- `apps/api/src/Foodeez.Application/UseCases/FoodItems/{SearchFoodItemsUseCase,CreateFoodItemUseCase}.cs`
- `apps/api/src/Foodeez.Domain/Entities/{MealLog,MealLogItem,FoodItem,MealTemplate,MealTemplateItem,DayAnalysis}.cs`
- `apps/api/src/Foodeez.Domain/ValueObjects/{NutritionalInfo,MealAnalysis}.cs`
- Clients: `apps/web/src/{views/meal-log,stores/meal.ts,services/mealService.ts}`,
  `apps/mobile/src/{screens/meal-log,store/mealStore.ts,services/mealService.ts,hooks/useMealItems.ts}`,
  and the reports screens/views on both sides.

## Main Flows

**Log / edit a meal** - `POST api/meal-logs` and `PUT api/meal-logs/{id}` share
`LogMealRequest`; both funnel through `MealLogRequestApplier.ApplyAsync`, which resolves food
items and replaces the item collection. `MealLog.TotalNutrition` sums the items, so nothing
stores a redundant total.

**Quick add (free text)** - `POST api/meal-logs/quick-add`: the model parses a description into
items (`ParseMealDescriptionAsync`), `ParsedMealResolver` matches them to existing `FoodItem`
rows or creates them, then the meal is logged.

**Photo** - `POST api/meal-logs/parse-image` sends the image to a vision-capable provider and
returns a parsed meal for the user to confirm before logging.

**Reading** - `GET api/meal-logs` (one day), `GET api/meal-logs/range`,
`GET api/meal-logs/nutrition-summary` (totals vs. the profile's targets),
`GET api/meal-logs/report` (`GetNutritionReportUseCase`, `NutritionReportDto`).

**Meal analysis** - `POST api/meal-logs/{id}/analysis` generates once, stores it on
`MealLog.Analysis`, and serves it from storage afterwards; `?refresh=true` forces a new one, and
editing the meal's items discards the old score by itself (`MealAnalysisFingerprint`).
`POST api/meal-logs/{id}/analysis/stream` is the same thing as server-sent events.

**Day analysis** - `GET/POST api/meal-logs/day-analysis` and `.../day-analysis/stream` produce a
`DayAnalysis` row (score, status, gaps, recommendations) keyed by user and date, with a
`Fingerprint` so an unchanged day is not re-analysed. `DayAnalysis.Matches(fingerprint)` is the
freshness check.

**Templates** - `api/meal-templates` lists, lists `recent`, creates, applies
(`POST {id}/apply` logs a meal from the template and bumps `TimesUsed` / `LastUsedAt`) and deletes.

**Food items** - `GET api/food-items/search` is DB-first: fewer than 3 local matches triggers a
USDA FoodData Central search, whose results are cached as `FoodItem` rows keyed by `FdcId` and
refreshed when `FdcSyncedAt` is more than 30 days old. `POST api/food-items` (authenticated)
creates one by hand.

## Data and State

Tables: `MealLogs` (user, `LogDate` as `date`, `MealType`, notes, owned `MealAnalysis`),
`MealLogItems` (quantity, unit, owned `NutritionalInfo`), `FoodItems` (with USDA `FdcId` and
`FdcSyncedAt`), `MealTemplates` + `MealTemplateItems`, `DayAnalyses`.

`NutritionalInfo` is a value object (owned type) rather than a table.

## External Systems

USDA FoodData Central through `IFoodDataService` / `UsdaFoodDataService` (retried), and
whichever AI provider is active for parsing and analysis (see `domains/ai-providers.md`).

## Tests and Validation

`apps/api/tests/Foodeez.Application.Tests/UseCases/` - `LogMealUseCaseTests`,
`UpdateMealLogUseCaseTests`, `DeleteMealLogUseCaseTests`, `GetNutritionSummaryUseCaseTests`,
`GetNutritionReportUseCaseTests`; `Common/NutritionMapperTests`, `MealTypeParsingTests`;
`Foodeez.Domain.Tests/{Entities/FoodItemTests,ValueObjects/NutritionalInfoTests}`.
Client side: `useMealItems.test.ts` on both web and mobile, `nutritionUtils.test.ts` (mobile),
`analysisScore.test.ts` (web).

## Known Gotchas

- Editing a meal used to fail as a concurrency conflict (commit `aa7311f`). The fix is the
  `ValueGenerated.Never` convention in `AppDbContext`: a new `MealLogItem` on a loaded
  `MealLog` must be inserted, not updated. Keep that in mind when adding child collections.
- `LogDate` is a `DateOnly` and maps to a `date` column by a model-wide convention. Clients
  send `yyyy-MM-dd` strings; do not introduce a `DateTime` here.
- An analysis is cached deliberately. If a change should invalidate it, extend the fingerprint
  rather than deleting the row from a use case.
- A blocking (non-streamed) AI call from mobile uses `AI_REQUEST_TIMEOUT` (120s), not the
  default axios timeout - a self-hosted model can be slow.
