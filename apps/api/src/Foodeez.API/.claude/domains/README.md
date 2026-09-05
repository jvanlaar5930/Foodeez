# Domain Notes

Detailed notes per domain. `.claude/project-map.md` stays the short high-level index; anything
long belongs here.

## Domains in this repository

| File | Covers |
|---|---|
| `auth.md` | Register, login, JWT issue/validate, identity per request, user profile, admin/active flags |
| `meal-logging.md` | Meal logs and items, quick-add and photo parsing, templates, food items, meal and day analysis, reports |
| `ai-providers.md` | `IAIService`/`IStreamingAIService`, runtime provider switching, the shared failure envelope, SSE |
| `recipes.md` | DB-first search with Spoonacular fallback and caching, lazy detail fill, saved recipes, filter tags |
| `meal-planning.md` | Meal plans and entries, slot rules on the entity, AI plan generation |
| `chat.md` | Conversations, streamed replies, accepting suggested meals and recipes |
| `grocery.md` | Compiling a list from planned meals, fingerprint-gated regeneration, manual edits |
| `admin-and-logging.md` | `AppSettings` runtime configuration, user administration, the DB log sink |
| `web-app.md` | `apps/web` - routes and guards, Pinia stores, axios/SSE services, UI primitives |
| `mobile-app.md` | `apps/mobile` - Expo navigation, Zustand stores, API URL resolution, camera |
| `shared-package.md` | `@foodeez/shared` - cross-client types, enums, nutrition and unit maths |
| `infrastructure.md` | Startup, DI, EF model and migrations, Docker, nginx, configuration precedence |

## Adding a Domain File

Create one only for a domain that is clearly present in the code, and add a row above and in
`.claude/project-map.md`'s domain table.

## Suggested Domain File Template

```md
# Domain: <name>

## Purpose

Needs verification.

## Key Files

Needs verification.

## Main Flows

Needs verification.

## Data and State

Needs verification.

## External Systems

Needs verification.

## Tests and Validation

Needs verification.

## Known Gotchas

Needs verification.
```
