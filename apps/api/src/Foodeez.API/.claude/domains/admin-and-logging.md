# Domain: admin-and-logging

## Purpose

The administrator surface - runtime settings (most importantly which AI provider is active),
user administration, and an application log stored in the database and readable from the UI.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/AdminController.cs` (route `api/admin`,
  `[Authorize(Roles = "Admin")]`) and `LogsController.cs` (route `api/logs`, same role).
- `apps/api/src/Foodeez.Application/UseCases/Admin/` - `AdminSettingsUseCase`,
  `AdminUsersUseCase`, `AppLogsUseCase`.
- `apps/api/src/Foodeez.Infrastructure/Services/{DbAppLogger,LogQueue,LogWriterService}.cs`
- `apps/api/src/Foodeez.Application/Interfaces/Services/IAppLogger.cs`
- `apps/api/src/Foodeez.API/Middleware/ExceptionHandlingMiddleware.cs` (the main producer).
- `apps/api/src/Foodeez.Domain/Entities/{AppSetting,AppLog}.cs`
- Clients: `apps/web/src/views/admin/{AdminLogsView,AdminUsersView,AdminSettingsView}.vue`,
  `apps/web/src/services/adminService.ts`. There is no admin surface in the mobile app.

## Main Flows

**Settings** - `GET api/admin/settings` lists the `AppSettings` rows;
`POST api/admin/settings` upserts many at once (`UpsertSettingsRequest`). Values marked
`IsSecret` are masked on the way out by `AdminSettingsUseCase.Mask`: first four and last four
characters, or `****` when the value is eight characters or shorter (masking a short value
would leave nothing hidden).

Known keys: `ai.provider` (read by `DynamicAIService`, default `claude`), the `local.*`
overrides for a self-hosted server, and `recipes.filterTags` (see `domains/recipes.md`).

**Users** - `GET api/admin/users`, `PUT api/admin/users/{id:guid}/toggle-admin`,
`PUT api/admin/users/{id:guid}/toggle-active`. `IsAdmin` becomes a role claim on the next token
the user is issued; `IsActive` is checked at login.

**Logs** - `GET api/logs` (paged), `GET api/logs/{id:long}`, `DELETE api/logs/prune`.
Writing is deliberately indirect: `IAppLogger` (`DbAppLogger`) pushes onto a singleton
`LogQueue`, and `LogWriterService` - an `IHostedService` - drains it into the `AppLogs` table,
so request handling never blocks on a log write.

## Data and State

- `AppSettings` - `Key` (primary key, not a `BaseEntity`), `Value`, `Category`, `Description`,
  `IsSecret`, `UpdatedAt`. This is runtime configuration, editable without a redeploy.
- `AppLogs` - `long Id`, timestamp, level (`Debug | Info | Warning | Error`), message, source,
  exception type/message/stack trace, request method/path, status code, user id, and an
  `AdditionalData` JSON blob. Added by the `AddAppLogs` migration.

## External Systems

None.

## Tests and Validation

`apps/api/tests/Foodeez.Application.Tests/UseCases/{AdminSettingsUseCaseTests,AdminUsersUseCaseTests}.cs`.

## Known Gotchas

- `AppSetting` and `AppLog` do **not** derive from `BaseEntity` - no `Guid` id, no
  `CreatedAt`/`UpdatedAt` convention. `AppSetting` is keyed by `Key`, `AppLog` by an
  auto-incrementing `long`.
- A masked secret must never be written back as a value. Treat a masked string arriving in an
  upsert as "unchanged" rather than storing the asterisks.
- `AppLogs` grows without bound; `DELETE api/logs/prune` is the only cleanup and is manual.
- The queue is in-memory, so logs buffered at the moment of an abrupt shutdown are lost, and a
  multi-instance deployment would have one queue per process.
- Changing `ai.provider` takes effect on the next request scope - `DynamicAIService` caches the
  resolved provider for the life of one request.
