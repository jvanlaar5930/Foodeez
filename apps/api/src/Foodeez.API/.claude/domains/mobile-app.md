# Domain: mobile-app

## Purpose

`apps/mobile` - the Android-first Expo client. Same API, same shared types as the web app, but
its own navigation, state and component set.

## Key Files

- `apps/mobile/App.tsx` and `index.js` - entry; `app.json`, `babel.config.js` (module resolver
  for `@`), `metro.config.js`.
- `apps/mobile/src/navigation/` - `RootNavigator` (auth vs. main, driven by
  `useAuthStore.isAuthenticated`), `AuthNavigator`, `MainTabNavigator`, and `types.ts` for the
  param lists.
- `apps/mobile/src/constants/api.ts` - `API_URL` resolution and `AI_REQUEST_TIMEOUT` (120s).
- `apps/mobile/src/services/` - `api.ts` (axios), `aiStream.ts` (SSE), `authBridge.ts`, and one
  service per domain, mirroring the web app's list.
- `apps/mobile/src/store/` - Zustand stores: `authStore`, `mealStore`, `mealPlanStore`,
  `groceryStore`, `chatStore`, `profileStore`, `savedRecipeStore`, plus `asyncState.ts`.
- `apps/mobile/src/components/ui/` - `Button`, `Card`, `Input`, `SearchBar`, `ModalSheet`,
  `ModalActions`, `ScreenHeader`, `EmptyState`, `ErrorBanner`, `LoadingSpinner`, `MacroRow`,
  `NutritionBar`, `DateRangeNav`.
- `apps/mobile/src/theme/index.tsx` - the theme provider; preference persisted in
  `expo-secure-store`.
- `apps/mobile/src/hooks/` - `useCamera`, `useFoodSearch`, `useMealAnalysis`, `useMealItems`,
  `useNutrition`.

## Main Flows

**Navigation** - a bottom tab navigator with seven tabs (Dashboard, Meal Log, Meal Plan,
Grocery, Ask AI, Recipes, Profile); Dashboard, Meal Log, Meal Plan, Recipes and Profile each
wrap a native stack (Reports, AddMeal/FoodScan, CalendarDay, RecipeDetail, EditProfile).
`RootNavigator` shows a spinner while the auth store rehydrates, then either `AuthNavigator`
or `MainTabNavigator`.

**Finding the API** (`constants/api.ts`) - `EXPO_PUBLIC_API_URL` wins if set (and must include
the `/api` suffix). Otherwise the host is taken out of the Expo dev-server URI, since the
bundle demonstrably reached the device from that address; if that host is loopback or absent,
it falls back to `10.0.2.2` on Android (the emulator's alias for the host) or `localhost`.

**State** - screens read Zustand stores with **per-field selectors**
(`useMealStore((s) => s.dailyLogs)`), not whole-store reads, to avoid re-rendering a screen on
every unrelated change. Async work goes through `runAsync`, which owns `isLoading` and `error`.

**Camera and photo logging** - `expo-camera` / `expo-image-picker` via `useCamera`, feeding
`FoodScanScreen` and the parse-image endpoint.

**Auth** - the JWT lives in `expo-secure-store`; `authBridge.ts` hands the token to the axios
instance without a circular import.

## Data and State

Zustand only, no persistence layer beyond `expo-secure-store` for the token and theme.
Nutrition and date helpers live in `src/utils/`; cross-client models come from `@foodeez/shared`
(re-exported through `src/types/`).

## External Systems

The Foodeez API. `expo-notifications` is installed, but the server side of push is a stub
(`NotificationService`).

## Tests and Validation

Jest with the `jest-expo` preset and `@testing-library/react-native`; setup in
`src/test/setup.ts`, a render helper in `src/test/render.tsx`. Existing tests:
`AddMealScreen.test.tsx`, `components/ui/{LoadingSpinner,primitives}.test.tsx`,
`hooks/useMealItems.test.ts`, `utils/{apiError,nutritionUtils}.test.ts`. `npm run mobile:test`.

## Known Gotchas

- The `/api` suffix is part of `EXPO_PUBLIC_API_URL`; without it every call 404s and login
  fails in a way that looks like bad credentials.
- A tunnelled dev server (`npm run mobile:tunnel`) is not an address the API is reachable on -
  set `EXPO_PUBLIC_API_URL` to the machine's LAN IP for that session.
- Blocking AI calls use `AI_REQUEST_TIMEOUT` (120s); the ordinary request timeout is much
  shorter and would cut off a slow self-hosted model mid-answer.
- Jest maps `@foodeez/shared` to the shared **source**, while the packaged app resolves the
  built `dist` - run `npm run shared:build` after changing shared types.
- Lists use `FlatList` with memoised rows for a reason (commit `4434b48`); do not replace one
  with a `map` over a `ScrollView`.
- Most "cannot connect" reports are Metro on port 8081, not the API - the README's
  troubleshooting section walks through it.
- The mobile app has no admin surface and is excluded from the Docker build (`.dockerignore`).
