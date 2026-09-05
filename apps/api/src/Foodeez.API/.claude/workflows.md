# Workflows: Foodeez

Step-by-step procedures. Paths are relative to the repo root (`c:\dev\Foodeez`).

## Add a New Feature

**A new API endpoint**

1. Domain first: if there is a new rule or field, put it on the entity in `Foodeez.Domain`
   (and add a migration - see below).
2. Add the DTOs in `Foodeez.Application/DTOs/<Area>/` (`...Dto`, `...Request`).
3. Add `Foodeez.Application/UseCases/<Area>/<Verb><Noun>UseCase.cs`, taking `IUnitOfWork` (and
   any `I...Service`) by constructor. No registration line is needed - `AddApplication()` picks
   up the `UseCase` suffix. A helper that needs injecting must end in `Resolver` or `Reader`.
4. Add repository query methods to the interface in `Application/Interfaces/Repositories` and
   implement them in `Infrastructure/Repositories`.
5. Add the action to the controller: thin body, `UserId` from the base class,
   `[ProducesResponseType]`, an XML `<summary>`, a `CancellationToken` if it reaches a model.
6. Extend `packages/shared/src/types/` if both clients need the model, then wire the client
   service (`apps/web/src/services/`, `apps/mobile/src/services/`) and store.
7. Tests: a use-case test at minimum (see next section).

**A new AI-backed feature**

1. Add the prompt builder to `Application/Common/<Feature>Prompt.cs`.
2. Add the method to `IAIService` (and `IStreamingAIService` if it streams), implement it once
   in `AIProviderBase` using `ExecuteAsync`/`RunAsync` with a fallback value, and add the
   delegating member to `DynamicAIService`. Only genuinely provider-specific HTTP shapes belong
   in the individual provider classes.
3. For streaming, expose `ExecuteStreamAsync` returning `IAsyncEnumerable<AIStreamEvent>` and
   have the controller call `ServerSentEventStream.WriteAsync`.
4. Decide whether the result should be cached in a table and what invalidates it (look at
   `MealAnalysisFingerprint` / `GroceryListFingerprint` for the existing pattern).

**A new screen or view**

- Web: add the view under `apps/web/src/views/<area>/`, register a lazy route with the right
  `meta` (`requiresAuth`, `requiresGuest`, `requiresAdmin`) in `apps/web/src/router/index.ts`,
  and add the sidebar entry in `components/layout/AppSidebar.vue`.
- Mobile: add the screen under `apps/mobile/src/screens/<area>/`, then register it in
  `navigation/MainTabNavigator.tsx` (or the relevant stack) and add its params to
  `navigation/types.ts`.

## Add or Update a Test

- API: put the test in the project that owns the behaviour (Domain / Application /
  Infrastructure / Integration), name it `<Thing>Tests`, use xUnit + FluentAssertions, mock
  interfaces with Moq. Run `cd apps/api && dotnet test`, or one project:
  `dotnet test tests/Foodeez.Application.Tests`.
- Shared and web: `*.test.ts` beside the code, Vitest. `npm run shared:test`, `npm run web:test`.
- Mobile: `*.test.ts(x)` beside the code, Jest. `npm run mobile:test`; render through
  `src/test/render.tsx`.
- Adding a controller or a constructor dependency: `ControllerDependencyTests` already proves
  every controller resolves - run the integration project after changing DI.

## Trace a Bug

1. Reproduce and note the HTTP status. The status tells you where to look:
   404/400/401/409/503 come from `ExceptionHandlingMiddleware`'s mapping table
   (see `.claude/architecture.md`), a 500 means an unmapped exception.
2. Admin > Logs (`GET /api/logs`) reads the `AppLogs` table, which
   `ExceptionHandlingMiddleware` writes to with the method, path, user id and status.
3. Follow controller then use case then repository. Business rules are on the entity; the
   controller almost never holds logic.
4. For AI faults, check which provider is actually active (`ai.provider` in Admin > Settings) -
   the log line is prefixed with the provider name. Remember that provider failures return a
   fallback rather than throwing.
5. For a stream that stops silently, suspect the transport: Kestrel's data-rate limit (disabled
   in `Program.cs`), nginx `proxy_read_timeout` / `proxy_buffering`, or a client abort.
6. Client side: `apps/*/src/utils/apiError.ts` is where a server message becomes UI text.

## Modify Data or State-Related Code

Schema change:

```
cd apps/api
dotnet ef migrations add <Name> --project src/Foodeez.Infrastructure --startup-project src/Foodeez.API
dotnet ef database update --startup-project src/Foodeez.API
```

- Requires `dotnet tool install --global dotnet-ef`.
- Commit the migration, its `.Designer.cs`, and the updated `AppDbContextModelSnapshot.cs`.
  Never hand-edit generated files.
- Put the mapping in `Infrastructure/Data/Configurations/<Entity>Configuration.cs`. Only
  model-wide conventions belong in `AppDbContext.OnModelCreating`.
- New entity: derive from `BaseEntity` so the id, timestamps and the `ValueGenerated.Never`
  convention apply; add the `DbSet`, a configuration, a repository (extending
  `BaseRepository<T>`), the interface, the DI registration, and the `IUnitOfWork` property.
- In Development the API applies pending migrations at startup, so restarting the API is
  usually enough. Deployed, that is `Database:AutoMigrate` / `DB_AUTO_MIGRATE`.

## Work With Configuration and Secrets

- New **non-secret** setting: add it to `appsettings.json` with a comment saying what it does,
  read it with `IConfiguration`, and add it to the README's environment-variable table.
- New **secret**: never put the value in a committed file.
  - Development: `cd apps/api/src/Foodeez.API && dotnet user-secrets set "Section:Key" "<value>"`
    (`dotnet user-secrets list` shows what is set).
  - Deployed: an environment variable with `:` replaced by `__`, plus a keyless entry in
    `.env.example` and the matching `Section__Key: ${VAR}` line in `docker-compose.yml`.
- If the API genuinely cannot run without it, add the key to `RequiredConfiguration.Validate`
  so startup fails with the key's name.
- Runtime-editable settings (the active AI provider, `local.*` overrides) belong in the
  `AppSettings` table via Admin > Settings, not in a file.
- Mobile: only `EXPO_PUBLIC_API_URL` in `apps/mobile/.env`, and the `/api` suffix is part of it.

## Update External Integrations

- New AI provider: subclass `AIProviderBase` (supply `ProviderName`, `SendAsync`, `StreamAsync`),
  register it with `services.AddHttpClient<...>()` in `Infrastructure/DependencyInjection.cs`,
  and add its name to the switch in `DynamicAIService.ResolveAsync`. Do **not** add a resilience
  handler to an AI client. Add its config section to `appsettings.json` and its key to
  `.env.example` and `docker-compose.yml`.
- Non-AI upstream (idempotent GETs): typed `HttpClient` **with** `.AddStandardResilienceHandler()`,
  behind an interface in `Application/Interfaces/Services`.
- Changing a prompt: edit the builder in `Application/Common` and update its test
  (`MealPlanPromptTests`, `RecommendationsPromptTests` are the models to follow).

## Build and Run Locally

First time:

```
npm install                                   # repo root
npm run shared:build
cd apps/api/src/Foodeez.API                   # then dotnet user-secrets set ... (README)
cd apps/api && dotnet restore
dotnet ef database update --startup-project src/Foodeez.API
```

Every day - three terminals, or `start-all.bat`:

| What | Where | Command | URL |
|---|---|---|---|
| API | `apps/api` | `dotnet run --project src/Foodeez.API` | http://localhost:5000 (`/swagger`, `/health`) |
| Web | repo root | `npm run web` | http://localhost:3000 |
| Mobile | repo root | `npm run android` (emulator) or `npm run mobile` (Expo Go) | Metro on 8081 |

MySQL: `docker start foodeez-mysql`, or the Windows service. `npm run mobile:tunnel` when the
LAN path to Metro is blocked (then set `EXPO_PUBLIC_API_URL` by hand for that session).

## Validate Before Commit

There is no CI, so run what your change touched:

```
cd apps/api && dotnet test        # API change
npm run shared:test               # packages/shared
npm run web:test                  # apps/web
npm run mobile:test               # apps/mobile
npm run web:build                 # vue-tsc type check + production build
npm test                          # every workspace at once
```

Also confirm: no secret in a diff; a schema change carries its migration; a config change is
reflected in `appsettings.json` / `.env.example` / `docker-compose.yml` / README together; and
durable project knowledge is recorded per `.claude/update-project-map.md`.

## Deployment or Release

1. Copy `.env.example` to `.env` and fill in real values (never commit it).
2. `docker compose up --build` - starts MySQL (with a healthcheck the API waits on), the API
   image from `apps/api/Dockerfile`, and nginx from `apps/web/Dockerfile` serving the built web
   app on `WEB_PORT` (default 8080) and proxying `/api/` to the API.
3. The API applies pending migrations on start unless `DB_AUTO_MIGRATE=false`.
4. Verify: `docker compose ps` (both healthchecks), then `/health` through the proxy and a
   sign-in from the web app.
5. Swagger is Development-only, so it is deliberately absent from a deployed stack.
6. The mobile app ships through Expo, not this compose stack.

No versioning, tagging, changelog or release automation exists in the repo. `Needs verification`
whether a release process is defined elsewhere.

## Rollback or Recovery

- Application: `docker compose down` then redeploy the previous image/commit.
- Schema: EF has no automatic down-migration in this stack.
  `dotnet ef database update <PreviousMigration> --startup-project src/Foodeez.API` reverts if
  the migration's `Down` is sound - check it first, and take a backup, because MySQL DDL is not
  transactional. Set `DB_AUTO_MIGRATE=false` before rolling an app version back past a migration.
- Data: the database lives in the `mysql-data` Docker volume. There is no backup job in the
  repo - `Needs verification` whether one exists outside it.
- Signing key: changing `Jwt:Key` invalidates every issued token and signs everyone out.
