# Domain: web-app

## Purpose

`apps/web` - the browser client. Vue 3 SPA, built by Vite, served in production by nginx from
the same origin as the API.

## Key Files

- `apps/web/src/main.ts` - creates the app, installs Pinia and the router, and resolves the
  theme before mounting so there is no light-mode flash.
- `apps/web/src/router/index.ts` - all routes (lazy-loaded) and the `beforeEach` guard.
- `apps/web/src/services/api.ts` - the axios instance (`baseURL: '/api'`), bearer-token
  interceptor, and 401 handling. `getAuthToken()` is exported because streaming endpoints are
  read with `fetch`, not axios.
- `apps/web/src/services/aiStream.ts` - the SSE reader.
- `apps/web/src/stores/` - `auth`, `meal`, `mealPlan`, `grocery`, `chat`, `profile`, `theme`,
  and `asyncState.ts` (the shared loading/error envelope).
- `apps/web/src/components/ui/` - `AppButton`, `AppCard`, `AppInput`, `AppAlert`, `AppModal`,
  `AppToggle`, `LoadingSpinner`, `EmptyState`, `ScoreDonut`, `ThemeToggleButton`.
- `apps/web/src/composables/` - `useAuth`, `useDebouncedSearch`, `useMealItems`,
  `usePagedRecipes`, `useRecipeDetail`, `useRecipeFilterTags`, `useScrollLock`.
- `apps/web/src/utils/` - `apiError`, `analysisScore`, `dateRange`, `macroColors`.
- `apps/web/vite.config.ts`, `apps/web/Dockerfile`, `apps/web/nginx.conf`.

## Main Flows

**Routing and guards** - routes carry `meta: { requiresAuth | requiresGuest | requiresAdmin }`.
The guard hydrates the current user when a token exists but the user is not loaded, redirects
unauthenticated users to `/auth/login?redirect=...`, and bounces signed-in users away from the
guest-only pages. `/` and `/search` are public: recipe discovery never requires an account.
Unknown paths redirect to `/`.

**Views** - `home`, `search`, `auth` (login, register, profile setup), `dashboard`, `meal-log`,
`meal-plan`, `grocery`, `advice`, `recipes`, `reports`, `profile`, and `admin/{logs,users,settings}`.

**Data access** - a view calls a store, the store calls a service, the service calls axios.
Stores wrap every call in `useAsyncState`'s `run` / `runOrThrow`, which own `isLoading` and a
human-readable `error` message, so components do not manage their own flags.

**Errors** - `utils/apiError.ts` reads the API's `ProblemDetails` (`detail`, then `title`). The
axios response interceptor logs out and redirects on any 401 except from `/auth/login` and
`/auth/register`, whose 401 means "those credentials are wrong" and belongs on the form.

**Streaming** - AI endpoints are read with `fetch` plus `getAuthToken()`, not axios, and parsed
as SSE `delta` / `done` / `error` events.

## Data and State

Pinia stores only; the JWT lives in `localStorage` under `foodeez_token`. Theme preference is
persisted by the `theme` store. Charts are Chart.js via `vue-chartjs`; dates are `date-fns`.

## External Systems

The Foodeez API. In development Vite proxies `/api` to `http://localhost:5000`; in production
nginx proxies `/api/` to the API container, which makes the app same-origin and is why
`Cors:AllowedOrigins` is empty by design.

## Tests and Validation

Vitest, `node` environment, `src/**/*.test.ts` - `errorMessages.test.ts` (stores),
`apiError.test.ts`, `analysisScore.test.ts`, `useDebouncedSearch.test.ts`,
`useMealItems.test.ts`, `useScrollLock.test.ts`. Component rendering is not covered.
`npm run web:test`; `npm run web:build` type-checks with `vue-tsc`.

## Known Gotchas

- Vite aliases `@foodeez/shared` straight to `packages/shared/src/index.ts`, so a shared-package
  change is picked up without rebuilding - but the Docker build runs `npm run shared:build`
  first, so a type that only compiles in source will still break the image build.
- The dev server port is 3000 (`vite.config.ts` and the README). `start-all.bat` prints 5173 -
  treat the batch file as stale, `Needs verification`.
- Tailwind 4 is wired through `@tailwindcss/vite`; there is no `tailwind.config.js`.
- Reuse the `components/ui/` primitives - several recent commits exist purely to replace ad-hoc
  markup with them.
