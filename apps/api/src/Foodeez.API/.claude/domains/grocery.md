# Domain: grocery

## Purpose

Turning a date range of planned meals into a shopping list: the AI consolidates ingredients into
categorised items, and the user then ticks, edits, adds and removes them by hand.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/GroceryListsController.cs` (route `api/grocery-lists`).
- `apps/api/src/Foodeez.Application/UseCases/Grocery/` - `GenerateGroceryListUseCase`,
  `EditGroceryListUseCase`, `PlannedMealReader`, `GroceryListMapper`.
- `apps/api/src/Foodeez.Application/Common/{GroceryListPrompt,GroceryListFingerprint}.cs`
- `apps/api/src/Foodeez.Domain/Entities/{GroceryList,GroceryListItem}.cs`
- `apps/api/src/Foodeez.Infrastructure/Repositories/GroceryListRepository.cs`
- Clients: `apps/web/src/{views/grocery,components/grocery,stores/grocery.ts,services/groceryService.ts}`,
  `apps/mobile/src/{screens/grocery,store/groceryStore.ts,services/groceryService.ts}`.

## Main Flows

**Peek** - `GET api/grocery-lists?startDate=&endDate=` returns a `GroceryListStateDto`: the
stored list for that range (via `_generate.PeekAsync`) and how many meals are planned in it.
It costs no AI call, so the tab can ask on every load and whenever the range changes. An end
date before the start date is refused with `Failure("Pick an end date on or after the start
date.")`.

**Generate** - `POST api/grocery-lists/generate/stream` only, as server-sent events.
`PlannedMealReader` collects the planned meals for the range, `GroceryListFingerprint` hashes
them, and a list already compiled from exactly those meals is returned complete with no deltas.
Only an explicit `refresh` spends an AI call on a range that has not changed.

**Edit** - `EditGroceryListUseCase` behind
`POST /{listId}/items`, `PUT /{listId}/items/{itemId}`,
`PATCH /{listId}/items/{itemId}/checked`, `DELETE /{listId}/items/{itemId}`.

## Data and State

`GroceryLists` (user, `StartDate`/`EndDate` as `date`, `Fingerprint`, `GeneratedAt`) and
`GroceryListItems` (name, free-text `Quantity`, `Category` defaulting to `"Other"`, `Source`,
`IsChecked`, `IsCustom`, `SortOrder`). `GroceryList.Matches(fingerprint)` is the freshness
check. `IsCustom` marks an item the user added that the plan never knew about, so a regeneration
can treat it differently from a generated one.

## External Systems

The active AI provider through `IStreamingAIService`; the source data is meal-plan entries
(see `domains/meal-planning.md`).

## Tests and Validation

`Needs verification` - no dedicated grocery tests exist in `apps/api/tests`.

## Known Gotchas

- `Quantity` is a string ("2 tbsp", "1 large"), not a number - the AI produces human quantities
  and they are stored as written.
- Regeneration is fingerprint-gated on purpose. If a change should force a new list, it belongs
  in `GroceryListFingerprint`, not in a caller deciding to pass `refresh`.
- The peek endpoint must stay free of AI calls; it is polled by the UI.
- There is one list per user per range; generating for an overlapping-but-different range
  produces a separate list.
