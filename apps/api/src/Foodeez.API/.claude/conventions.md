# Conventions: Foodeez

Patterns already established in the code. Follow these rather than introducing new ones.

## Naming Conventions

**Backend (C#)**

- Namespaces mirror folders: `Foodeez.Application.UseCases.MealLogs`. File-scoped `namespace`.
- One use case per file, class named `<Verb><Noun>UseCase` with a single public
  `ExecuteAsync(...)`. Streamed variants are `ExecuteStreamAsync(...)`.
- The two collaborator kinds that read data for a use case end in `Resolver` (`ParsedMealResolver`)
  or `Reader` (`PlannedMealReader`). **These three suffixes are load-bearing** - `AddApplication()`
  registers by them, so a helper named anything else will not be injectable.
- DTOs end in `Dto`; inbound bodies end in `Request` (`LogMealRequest`, `UpsertSettingsRequest`).
- Repository interfaces `I<Entity>Repository`; service interfaces `I<Thing>Service`.
- Static mapper classes named `<Thing>Mapper` with `ToDto` / `MapToDto` methods.
- Prompt builders live in `Application/Common` as `<Feature>Prompt` (`MealPlanPrompt`).
- Controllers `<Plural>Controller`, route `[Route("api/kebab-case")]`.

**Clients (TypeScript)**

- Web: views `<Name>View.vue`, shared primitives `App*.vue` in `components/ui/`, Pinia stores
  `use<Name>Store` in `stores/<name>.ts`, composables `use<Name>` in `composables/`.
- Mobile: screens `<Name>Screen.tsx`, primitives in `components/ui/` (`Button`, `Card`,
  `ModalSheet`, ...), Zustand stores `use<Name>Store` in `store/<name>Store.ts`.
- Both alias `@` to their own `src/` and import cross-client models from `@foodeez/shared`.

## Project Organization

- Clean Architecture boundaries are real: never reference EF Core or ASP.NET types from
  `Foodeez.Domain` or `Foodeez.Application`. Declare an interface in Application and implement
  it in Infrastructure.
- Business rules belong on the entity (see `User.Create`, and the entities touched by
  "move three rules into the entities that own them"), not in the use case.
- Controllers stay thin: resolve `UserId`, validate arguments, call one use case, return.
- Large screens/views get split rather than grown - the recent history is full of
  `refactor(mobile): split AddMealScreen, 847 lines down to 387`. Extract a component into the
  feature folder, or a composable/hook, before a file passes a few hundred lines.

## Dependency Management

- NuGet versions live **only** in `apps/api/Directory.Packages.props`
  (`ManagePackageVersionsCentrally`). A `.csproj` lists `PackageReference` without `Version`.
- EF Core, the EF design package and Pomelo move together and stay on 9.x.
  Swashbuckle is held at 7.2.0 on purpose. Read the comments before bumping either group.
- npm: one lockfile at the repo root, three workspaces. Install from the root only.

## Application Structure

- Constructor injection with `private readonly` fields; no primary constructors, no service
  locator (the one exception is `DynamicAIService`, which resolves a provider by name).
- Everything application-scoped is `AddScoped` - use cases, repositories, unit of work,
  `DynamicAIService`. `IJwtService` and the log queue are singletons.
- Use cases talk to data through `IUnitOfWork` (`_unitOfWork.MealLogs`, ...) and call
  `SaveChangesAsync()` once at the end, not through a `DbContext` directly.

## Testing Conventions

- xUnit + FluentAssertions (`result.Should().Be(...)`), Moq for interfaces. One test class per
  unit, `<Thing>Tests`, mirroring the source folder (`UseCases/LogMealUseCaseTests.cs`).
- Test the layer that owns the rule: entity rules in `Foodeez.Domain.Tests`, orchestration in
  `Foodeez.Application.Tests`, EF/provider behaviour in `Foodeez.Infrastructure.Tests`,
  wiring in `Foodeez.Integration.Tests`.
- Integration tests use `FoodeezWebApplicationFactory` (real `Program`, EF in-memory provider).
- Web/shared: Vitest, `*.test.ts` next to the code, `node` environment - stores and pure
  functions, not rendered components.
- Mobile: Jest with `jest-expo`, `*.test.ts(x)` next to the code, rendering covered through
  `@testing-library/react-native` and the helper in `src/test/render.tsx`.

## Logging Conventions

- Structured templates with named placeholders: `Logger.LogError(ex, "{Provider}: failed to
  {Activity}.", ProviderName, activity)`. Never string interpolation into a log message.
- `ILogger<T>` for console/host logging; `IAppLogger` when the entry should also land in the
  `AppLogs` table for Admin > Logs.
- An expected outcome is a warning, an unexpected one an error. A cancelled AI stream is a
  warning, deliberately, because a reader closing a tab is common.

## Error Handling Conventions

- Throw the exception whose mapping you want: `KeyNotFoundException` for a missing row,
  `ArgumentException` / `InvalidOperationException` / `ValidationException` for bad input,
  `ConcurrencyConflictException` for a lost update, `AIGenerationFailedException` when a model
  produced nothing usable. `ExceptionHandlingMiddleware` turns each into the right status.
- For a refusal the action can see coming, return `BadRequest(Failure("..."))` - never a bare
  string and never `{ message }`; the clients only read `detail` and `title`.
- Never swallow `OperationCanceledException` - rethrow it.
- Clients funnel every failure through `utils/apiError.ts` so one reader handles all shapes.

## Data Access Conventions

- Repositories extend `BaseRepository<T>` (`GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`,
  `Delete`) and add query methods for their own aggregate.
- Repositories never call `SaveChangesAsync`; the use case does, through `IUnitOfWork`.
- Entity configuration goes in `Infrastructure/Data/Configurations/<Entity>Configuration.cs`,
  not in `OnModelCreating` - only genuinely model-wide conventions live there.
- List-shaped columns use the `JsonStringList` / `JsonValueList` converters.
- Read-only queries should be no-tracking; batch related reads rather than querying per item.
- Every schema change needs a migration (see `.claude/workflows.md`); generated `*.Designer.cs`
  and the model snapshot are committed but never hand-edited.

## API or Interface Conventions

- Routes are kebab-case under `/api` (`api/meal-logs`, `api/grocery-lists`,
  `api/meal-templates`). Route parameters are constrained: `{id:guid}`, `{id:long}`.
- Controllers derive from `FoodeezController` (which carries `[ApiController]`) and are
  `[Authorize]` at class level; public endpoints opt out with `[AllowAnonymous]` per action.
  Admin controllers use `[Authorize(Roles = "Admin")]`.
- Every action carries `[ProducesResponseType]` for the shapes it can return, and an XML
  `<summary>` - Swagger is the API's documentation.
- Enums serialize as strings (`JsonStringEnumConverter`); JSON is camelCase.
- Streaming endpoints are `POST .../stream`, `[Produces("text/event-stream")]`, and delegate to
  `ServerSentEventStream.WriteAsync`. Events are `delta`, `done`, `error` (`AIStreamEvent`).
- Take a `CancellationToken` on anything that reaches a model or an upstream API.

## Frontend or UI Conventions

- Web: Vue 3 SFCs, `<script setup lang="ts">`, Tailwind utility classes inline, dark mode via
  `dark:` variants and the `theme` store (resolved before first paint to avoid a flash).
- Reuse the primitives in `components/ui/` (`AppButton`, `AppCard`, `AppInput`, `AppAlert`,
  `AppModal`, `LoadingSpinner`, `EmptyState`) rather than restyling a raw element.
- Stores wrap async work in `useAsyncState` / `runAsync`, which own `isLoading` and a
  human-readable `error` string; components read those instead of managing their own flags.
- Mobile mirrors the same idea with `components/ui/` primitives and the `theme/` provider;
  lists use `FlatList` with memoised rows, and stores are read with per-field selectors to
  avoid re-rendering a whole screen.
- Never take the user id from a screen's own state where the API can take it from the token.

## Infrastructure Conventions

- Config keys use the section:key form and are read with `IConfiguration`; anything required is
  added to `RequiredConfiguration.Validate`.
- Non-secrets go in `appsettings.json` where a diff can review them; secrets go to user-secrets
  or environment variables, and are also added to `.env.example` **without a value**.
- Docker builds copy `.csproj` files first for layer caching; the API image runs as `app` and
  exposes a `/health` check.

## Documentation Conventions

- Code comments explain *why*, at length, and are treated as part of the change. The codebase
  routinely documents the alternative that was rejected and the failure it caused
  (`Directory.Packages.props`, `DependencyInjection.cs`, `AppDbContext.cs`, `api.ts`). Match
  that register: a comment that only restates the line below it does not belong.
- XML doc comments on public use cases, controllers actions, and anything with a subtle contract.
- Commit messages are Conventional Commits with a scope: `feat(meal-plan):`, `fix(api):`,
  `refactor(web):`, `perf(mobile):`, `test(mobile):`, `chore(api):`, subject in lower case and
  written as prose ("stop meal log edits failing as concurrency conflicts").
- `README.md` is the setup and troubleshooting document and is kept current with config changes.
