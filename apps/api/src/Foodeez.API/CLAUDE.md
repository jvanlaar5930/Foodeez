<!-- BEGIN create-claude-repo-memory -->
# Claude Instructions for Foodeez.API

Before answering project-specific questions:

1. Read `.claude/project-map.md`.
2. Use the project map to identify the relevant domain, files, and conventions.
3. Read only the specific files needed for the task.
4. Do not scan generated, build, cache, vendor, dependency, or IDE folders unless explicitly asked.
5. Prefer existing project patterns over introducing new architecture.
6. When changing code, identify tests or validation steps that should be added or updated.
7. When discovering durable project knowledge, update the most specific relevant file under `.claude/`.

## Project Memory Workflow

Use this order for project-specific work:

1. Read `.claude/project-map.md`.
2. If the task is domain-specific, read the relevant file under `.claude/domains/` if one exists.
3. Read `.claude/conventions.md` before making code-style or pattern decisions.
4. Read `.claude/workflows.md` before changing build, test, deployment, release, data, or integration workflows.
5. Read source files only after narrowing the task to the relevant area.

## Project Memory Update Rule

After making changes, decide whether the change affects durable project knowledge.

Update `.claude/` memory files only when the change affects:

- repository structure
- architecture
- major domains
- entry points
- dependency flow
- data access
- external integrations
- build, test, deployment, or release workflow
- coding conventions
- operational concerns
- known gotchas

Do not update project memory for small local implementation changes.

When updating memory:

1. Update the most specific file possible.
2. Keep `.claude/project-map.md` concise.
3. Prefer domain files under `.claude/domains/` for detailed notes.
4. Do not duplicate source code.
5. Do not include secrets.
6. If two memory updates conflict, preserve the facts that match current code and remove stale statements.

## Do Not Scan By Default

Do not scan these unless directly relevant:

- `.git/`
- dependency folders
- build output folders
- generated files
- package/cache folders
- IDE/editor folders
- coverage/report folders
- local environment folders

Common examples include:

- `node_modules/`
- `vendor/`
- `bin/`
- `obj/`
- `dist/`
- `build/`
- `target/`
- `out/`
- `.next/`
- `.nuxt/`
- `.turbo/`
- `.cache/`
- `.pytest_cache/`
- `__pycache__/`
- `.venv/`
- `venv/`
- `.gradle/`
- `.idea/`
- `.vscode/`
- `.vs/`
- `coverage/`

## Project Memory Files

Claude-specific project context is stored in:

- `.claude/project-map.md`
- `.claude/architecture.md`
- `.claude/conventions.md`
- `.claude/workflows.md`
- `.claude/update-project-map.md`
- `.claude/domains/`

## Metrics Opt In / Opt Out

Users can opt out of terminal-only metrics for a session with:

```text
Read .claude/prompts/metrics-opt-out.md and follow it for this session.
```

Users can opt back in with:

```text
Read .claude/prompts/metrics-opt-in.md and follow it for this session.
```

Opting out of metrics does not disable repo memory. It only disables the terminal/chat metrics summary.
<!-- END create-claude-repo-memory -->

## Project Facts (Foodeez)

Filled from the repository on 2026-09-04. These memory files sit inside `Foodeez.API` but
describe the **whole Foodeez repository**; the repo root is four levels up
(`apps/api/src/Foodeez.API/` -> `c:\dev\Foodeez`), and every path in them is written relative
to that root.

Foodeez is an AI-assisted food tracking and meal prepping app: a .NET 10 / MySQL API
(Clean Architecture, `apps/api`) serving a Vue 3 web app (`apps/web`) and an Expo/React Native
app (`apps/mobile`), with shared TypeScript models in `packages/shared`. npm workspaces at the
root; NuGet central package management under `apps/api`.

Read `.claude/project-map.md` first. The five things worth knowing before touching anything:

1. **Layer boundaries are real.** Domain <- Application <- Infrastructure <- API. No EF Core or
   ASP.NET types in Domain or Application - declare an interface, implement it in Infrastructure.
2. **Use cases register themselves** by the `UseCase` / `Resolver` / `Reader` suffix
   (`Application/DependencyInjection.cs`). A helper named anything else will not be injectable.
3. **Never add a retry/resilience handler to an AI HTTP client** - non-idempotent, billed per
   attempt, unreplayable streams. Upstream GET APIs (Spoonacular, USDA) do get one.
4. **Entity ids are client-assigned** (`ValueGenerated.Never` in `AppDbContext`). Undoing that
   brings back phantom 409s on any edit that adds a child row.
5. **Errors have one shape**: throw the exception whose mapping you want, or return
   `Failure(detail, status)`. Clients read only `detail` and `title` from `ProblemDetails`.

Comments here explain *why*, and routinely record the rejected alternative and the failure it
caused. Match that register rather than adding comments that restate the code.

Validation, since there is no CI: `cd apps/api && dotnet test`, `npm run web:test`,
`npm run mobile:test`, `npm run shared:test`, `npm run web:build`. See `.claude/workflows.md`.
