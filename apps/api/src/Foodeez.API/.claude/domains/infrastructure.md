# Domain: infrastructure

## Purpose

How the application is configured, persisted, containerised and served: EF Core mapping and
migrations, the composition root, the Docker/nginx stack, and the startup contract.

## Key Files

- `apps/api/src/Foodeez.API/Program.cs`, `Configuration/RequiredConfiguration.cs`,
  `Properties/launchSettings.json`, `appsettings.json`, `appsettings.Development.json`.
- `apps/api/src/Foodeez.Infrastructure/DependencyInjection.cs` and
  `apps/api/src/Foodeez.Application/DependencyInjection.cs`.
- `apps/api/src/Foodeez.Infrastructure/Data/` - `AppDbContext.cs`,
  `DesignTimeDbContextFactory.cs`, `Configurations/*` (including `JsonStringList`,
  `JsonValueList`).
- `apps/api/src/Foodeez.Infrastructure/Migrations/` - 13 migrations plus the model snapshot.
- `apps/api/Directory.Packages.props`, `apps/api/Foodeez.sln`.
- `docker-compose.yml`, `.env.example`, `.dockerignore`, `apps/api/Dockerfile`,
  `apps/web/Dockerfile`, `apps/web/nginx.conf`, `start-all.bat`.

## Main Flows

**Startup** (`Program.cs`, in order): disable Kestrel's `MinResponseDataRate`; validate required
configuration; `AddInfrastructure`; `AddApplication`; JWT bearer; Swagger with a bearer scheme;
CORS; controllers with `RequireTokenSubjectFilter` and `JsonStringEnumConverter`; health checks.
Then build, optionally migrate, and run the pipeline:
`ExceptionHandlingMiddleware` -> Swagger (Development) -> CORS -> authentication ->
authorization -> `MapControllers` -> `/health`. `Program` is `public partial` so integration
tests can boot it.

**Registration** - Infrastructure registers explicitly (DbContext with
`EnableRetryOnFailure(3, 10s)`, twelve repositories, `IUnitOfWork`, the AI providers, the two
resilience-wrapped upstream clients, `IJwtService` as a singleton, `INotificationService`, and
the log queue with its hosted writer). Application registers by convention over the `UseCase`,
`Resolver` and `Reader` suffixes.

**Configuration precedence** - `appsettings.json`, then `appsettings.{Environment}.json`, then
user-secrets (Development only), then environment variables (`:` becomes `__`).
`RequiredConfiguration.Validate` fails startup naming the missing key, with different advice per
environment. Provider API keys are deliberately not required, because the active provider is a
database setting.

**Migrations** - `dotnet ef migrations add <Name> --project src/Foodeez.Infrastructure
--startup-project src/Foodeez.API`. `DesignTimeDbContextFactory` lets the EF tools construct
the context. At runtime `Database:AutoMigrate` decides whether pending migrations are applied on
boot - unset means on in Development, off elsewhere - and the call is guarded by
`db.Database.IsRelational()` so the in-memory provider used by integration tests does not throw.

**Deployment** - `docker compose up --build` starts:

| Service | Image | Notes |
|---|---|---|
| `mysql` | `mysql:8.4` | `mysql-data` volume, `mysqladmin ping` healthcheck the API waits on |
| `api` | built from `apps/api/Dockerfile` | SDK build stage to `aspnet:10.0` runtime, runs as `app`, `curl`-based `/health` healthcheck, exposes 5000 (not published) |
| `web` | built from `apps/web/Dockerfile` | node build stage (`npm ci`, `shared:build`, web build) to `nginx:1.27-alpine`, published on `WEB_PORT` (default 8080) |

nginx serves the SPA (`try_files ... /index.html`) and proxies `/api/` to `http://api:5000/api/`
with `proxy_read_timeout`/`proxy_send_timeout` 600s and `proxy_buffering off`.

## Data and State

MySQL 8.4, one database. Model-wide conventions in `AppDbContext.OnModelCreating`: every
`DateOnly` maps to a `date` column, and every `BaseEntity.Id` is `ValueGenerated.Never`.
`SaveChangesAsync` stamps `UpdatedAt` on modified entities. Per-entity mapping lives in
`Data/Configurations/`. State outside the database: the `mysql-data` volume and the in-memory
log queue.

## External Systems

Docker, MySQL, nginx. See `domains/ai-providers.md` and `domains/recipes.md` for the outbound
HTTP integrations.

## Tests and Validation

- `apps/api/tests/Foodeez.Infrastructure.Tests/Data/EntityKeyGenerationTests.cs` - the
  `ValueGenerated.Never` convention.
- `apps/api/tests/Foodeez.Integration.Tests/` - `FoodeezWebApplicationFactory` (real `Program`,
  EF in-memory provider), `ControllerDependencyTests`, `AIServiceRegistrationTests`,
  `HealthCheckTests`.

## Known Gotchas

- EF Core, `Microsoft.EntityFrameworkCore.Design` and Pomelo are pinned to 9.x together; Pomelo
  9.0.0 is the newest MySQL provider and EF Core 10 alongside it breaks `DbContext`
  construction. Swashbuckle is held at 7.2.0 because 8 moves to Microsoft.OpenApi 2.x, which
  reshapes the types `Program.cs` builds its security scheme from.
- Package versions belong only in `Directory.Packages.props`; a `Version` attribute in a
  `.csproj` is an error under central package management.
- Auto-migration is per-instance and one-way. A multi-instance deployment should set
  `DB_AUTO_MIGRATE=false` and run migrations deliberately.
- The in-memory provider used by integration tests does not exercise the JSON converters,
  `date` column mapping, or MySQL collation, so those need a real database to verify.
- `.dockerignore` excludes `apps/mobile/`, `apps/api/tests/` and `*.md` - the API image's build
  context is `./apps/api`, while the web image's is the repo root.
- There is no CI pipeline, no image registry step and no release automation in this repo.
