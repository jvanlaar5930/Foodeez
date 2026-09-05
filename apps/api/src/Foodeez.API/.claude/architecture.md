# Architecture: Foodeez

Paths are relative to the repo root. See `.claude/project-map.md` for the index.

## High-Level Architecture

Three deployables around one database:

```
Vue web app (nginx) ─┐
                     ├─ HTTP/JSON + SSE ─> Foodeez.API (ASP.NET Core) ─> MySQL 8.4
Expo mobile app ─────┘                            │
                                                  ├─> Claude / Gemini / Groq / Ollama / LocalAI
                                                  ├─> Spoonacular (recipes)
                                                  └─> USDA FoodData Central (food items)
```

Both clients speak the same REST API under `/api`, share TypeScript models from
`@foodeez/shared`, and authenticate with the same JWT.

## Major Layers or Components

The API follows Clean Architecture, one .NET project per layer:

| Project | Contains | References |
|---|---|---|
| `Foodeez.Domain` | Entities (`BaseEntity` derived), enums, value objects (`NutritionalInfo`, `MealAnalysis`, `PlannedMeal`, `SuggestedRecipe`). Business rules live on the entities. | nothing |
| `Foodeez.Application` | Use cases, DTOs, repository/service **interfaces**, prompt builders, mappers, `IUnitOfWork`, `PagedResult`, custom exceptions. | Domain |
| `Foodeez.Infrastructure` | `AppDbContext` + entity configurations, repositories, AI providers, `JwtService`, `SpoonacularService`, `UsdaFoodDataService`, DB logging. | Application (and so Domain) |
| `Foodeez.API` | Controllers, `ExceptionHandlingMiddleware`, `RequireTokenSubjectFilter`, `ServerSentEventStream`, `RequiredConfiguration`, `Program.cs`. | all three |

## Dependency Flow

Domain <- Application <- Infrastructure <- API. Dependencies point inward: the Application
layer declares `IUserRepository`, `IAIService`, `IJwtService` and so on, and Infrastructure
supplies the implementations. Nothing in Domain or Application references EF Core or ASP.NET.

Registration happens in two extension methods called from `Program.cs`:

- `builder.Services.AddInfrastructure(configuration)` - DbContext, repositories, unit of work,
  AI providers, typed HTTP clients, JWT, logging sink and its hosted service.
- `builder.Services.AddApplication()` - reflection over the Application assembly, registering
  every public non-abstract class whose name ends in `UseCase`, `Resolver` or `Reader` as
  scoped. Adding a use case is therefore one file, with no registration line to forget;
  `ControllerDependencyTests` asserts every controller can still be constructed.

## Data Flow

Ordinary request:

```
Controller action (thin) -> UseCase.ExecuteAsync -> IUnitOfWork.<Repo> -> AppDbContext -> MySQL
                                                 -> Mapper -> DTO -> ProblemDetails on failure
```

Controllers hold no logic beyond argument checks and `UserId`. Use cases orchestrate, mappers
(`MealLogMapper`, `NutritionMapper`, `RecipeMapper`, `ChatMapper`, `GroceryListMapper`,
`SpoonacularRecipeMapper`, `UserProfileMapper`, `FoodItemMapper`) convert entity to DTO.

AI request (streamed):

```
Controller -> ServerSentEventStream.WriteAsync(Response, useCase.ExecuteStreamAsync(...), ct)
           -> DynamicAIService (resolves provider from AppSettings once per scope)
           -> <Provider>AIService -> provider HTTP API (streamed)
           -> AIStreamEvent { delta | done | error } written as SSE
```

Once the first SSE byte is written the status code is settled, so a mid-stream failure is
delivered as an `error` event rather than a 500.

## External Integrations

| Integration | Implementation | Notes |
|---|---|---|
| Claude (default) | `ClaudeAIService` | Model and `MaxTokens` from `Claude:*`; falls back to 8192 output tokens. |
| Gemini / Groq | `GeminiAIService`, `GroqAIService` | Selected by the `ai.provider` setting. |
| Ollama | `OllamaAIService` | `Ollama:BaseUrl`, `Ollama:Model`. |
| OpenAI-compatible local servers | `LocalAIService` | One client covers LM Studio, llama.cpp, vLLM, LocalAI, Jan. `HttpClient.Timeout` is infinite; the deadline is set per request instead. |
| Spoonacular | `SpoonacularService` | Typed `HttpClient` **with** `AddStandardResilienceHandler()`. |
| USDA FoodData Central | `UsdaFoodDataService` | Typed `HttpClient` **with** the standard resilience handler. |
| Push notifications | `NotificationService` | Stub - logs only. TODOs describe an FCM/Expo Push implementation. |

`DynamicAIService` implements both `IAIService` and `IStreamingAIService` and is registered
once for both, so streaming and non-streaming calls in one request share a resolved provider.
The provider name is read from the `AppSettings` row `ai.provider` and cached per scope
(per request), which removed a repeated SELECT on multi-call requests such as plan generation.

## Background, Scheduled, or Async Work

- `LogWriterService` (`IHostedService`) drains `LogQueue` into the `AppLogs` table, so request
  handling never blocks on writing a log row. `DbAppLogger` is the `IAppLogger` front end.
- Startup migration (`db.Database.Migrate()`), guarded by `db.Database.IsRelational()` so the
  in-memory provider used by integration tests does not throw.
- No scheduler, queue broker, or recurring job framework is present.

## Data Storage and State Management

- One MySQL database, EF Core code-first, entity configurations in
  `Infrastructure/Data/Configurations/` applied via `ApplyConfigurationsFromAssembly`.
- Two model-wide conventions in `AppDbContext.OnModelCreating`: every `DateOnly` maps to a
  `date` column, and every `BaseEntity.Id` is `ValueGenerated.Never`.
- `SaveChangesAsync` stamps `UpdatedAt` on modified `BaseEntity` instances.
- `JsonStringList` / `JsonValueList` value converters store list-shaped columns as JSON.
- AI-produced artifacts are cached in tables rather than regenerated: `MealLog.Analysis`,
  `DayAnalysis`, `GroceryList`, cached `Recipe` rows. Fingerprint helpers
  (`MealAnalysisFingerprint`, `GroceryListFingerprint`) decide when a cached result is stale.
- Clients hold their own state: Pinia stores (web) and Zustand stores (mobile), each with a
  small `asyncState` helper for loading/error envelopes.

## Error Handling and Logging

- `ExceptionHandlingMiddleware` is the outermost middleware. It logs through `ILogger` *and*
  `IAppLogger` (DB sink), then writes `application/problem+json`. Mapping:

  | Exception | Status |
  |---|---|
  | `KeyNotFoundException` | 404 |
  | `UnauthorizedAccessException` | 401 |
  | `ValidationException`, `ArgumentException`, `InvalidOperationException` | 400 |
  | `ConcurrencyConflictException` | 409 |
  | `AIGenerationFailedException` | 503 |
  | anything else | 500 |

- Expected refusals inside an action use `Failure(detail, status)` from `FoodeezController`,
  which returns the same `ProblemDetails` shape.
- AI provider failures do not normally throw: `AIProviderBase.RunAsync` logs and returns a
  fallback value, except for `OperationCanceledException`, which is rethrown so a cancelled
  request is never flattened into empty data the caller would treat as real.
- Clients read `detail` then `title` (`apps/*/src/utils/apiError.ts`).

## Security and Secrets Handling

- JWT bearer with full validation (issuer, audience, lifetime, signing key) and
  `ClockSkew = TimeSpan.Zero`.
- `RequireTokenSubjectFilter` runs for every action and rejects an authenticated request whose
  token carries no readable subject claim - `[Authorize]` alone does not guarantee one.
- User identity always comes from the token (`FoodeezController.UserId`), never from a query
  string parameter.
- Passwords are BCrypt hashed. Admin-only endpoints use `[Authorize(Roles = "Admin")]`.
- Secret values in the `AppSettings` table are masked before leaving the API
  (`AdminSettingsUseCase.Mask` - first four and last four characters, or `****` if short).
- Secrets never live in committed files; see `.claude/project-map.md` for where they come from.
- Config the API cannot run without is validated at startup by `RequiredConfiguration`.

## Performance or Scalability Notes

- Kestrel's `MinResponseDataRate` is disabled so long model pauses do not abort a stream;
  nginx uses `proxy_buffering off` with 600s read/send timeouts for the same reason.
- Recipe and food-item search are DB-first with a 30-day staleness threshold; upstream calls
  happen only when local results are thin. Recipe paging maps onto Spoonacular's own offset,
  so page N costs one upstream call.
- Cached recipe rows from `complexSearch` are deliberately partial (no ingredients or
  instructions); `GetRecipeDetailUseCase` fills a recipe in lazily when it is opened.
- Read-only queries use no-tracking where the meal planner previously queried per meal.
- The API is written for a single instance: startup auto-migration, the in-memory log queue,
  and the per-scope provider cache all assume one process owns the database.

## Architectural Risks or Unclear Areas

- No CI pipeline, linting, or formatting gate - correctness rests on `dotnet test` and the
  JS test suites being run by hand.
- Push notifications are declared (`INotificationService`) but not implemented.
- Multi-instance deployment would need `Database:AutoMigrate` off and a shared log sink;
  `Needs verification` whether that is ever intended.
- Integration tests use the EF in-memory provider, so MySQL-specific behaviour (the JSON
  converters, `date` column mapping, collation-sensitive search) is not covered end to end.
