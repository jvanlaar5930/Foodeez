# Updating the Project Map

Use this file as maintenance guidance for Claude project memory.

## When to Update

Update `.claude/` memory files when discovering or changing durable project facts such as:

- New major domains
- Important entry points
- Critical files
- Non-obvious configuration behavior
- Data access or state management patterns
- External integrations
- Testing or validation conventions
- Build, deployment, release, or operational behavior
- Known gotchas
- Significant architecture or dependency-flow changes

Do not update project memory for every code change.

## Where to Update

Use the most specific file possible:

- `.claude/project-map.md` for concise high-level index changes
- `.claude/architecture.md` for architecture, dependency flow, data flow, and integration design
- `.claude/conventions.md` for coding, naming, testing, logging, and organization patterns
- `.claude/workflows.md` for build, test, validation, deployment, release, debugging, and operational steps
- `.claude/domains/*.md` for detailed notes about specific business or technical domains

## Multi-Agent Memory Updates

When multiple agents are working concurrently:

- Do not update `.claude/project-map.md` unless the high-level project index changed.
- Prefer updating the relevant domain file under `.claude/domains/`.
- Keep memory changes in the same branch as the code change that caused them.
- If a memory file conflicts during merge, resolve it like documentation:
  - keep facts that match current code
  - remove stale facts
  - keep the shorter wording when both are correct
  - mark uncertain items as `Needs verification`

## Rules

- Keep summaries short and useful.
- Do not duplicate entire source files.
- Do not include secrets, credentials, tokens, private keys, connection strings, or sensitive values.
- Mark uncertain items as `Needs verification`.
- Do not invent facts.
- Prefer facts grounded in repository files over guesses.
- Remove stale statements when contradicted by code.
- Move detailed explanations out of `.claude/project-map.md` and into supporting memory files.

## Maintenance Workflow

1. Read `.claude/project-map.md`.
2. Inspect only the relevant project files.
3. Decide whether durable project knowledge changed.
4. If yes, update the smallest relevant memory section.
5. Keep the top-level project map concise.
6. Add detailed notes to supporting memory files when needed.

## Where Things Go In This Repository

Filled from the repository on 2026-09-04.

| A change to ... | Update |
|---|---|
| A new controller route, entity or npm/NuGet dependency | `project-map.md` (only the affected line) |
| Layering, DI, error mapping, streaming, an external integration's design | `architecture.md` |
| A naming rule, test pattern, comment or commit convention | `conventions.md` |
| Setup, migrations, secrets handling, validation commands, deployment | `workflows.md` |
| Anything inside one feature area | the matching file in `domains/` |
| A new AI provider | `domains/ai-providers.md`, plus `project-map.md`'s tech-stack row |
| A new npm workspace or .NET project | `project-map.md` layout **and** a new `domains/` file |

Facts that go stale fastest here, and are worth re-checking when you touch them: the pinned
package versions and the reasons in `Directory.Packages.props`, the migration count, the route
list, and anything currently marked `Needs verification`.
