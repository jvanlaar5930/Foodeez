# Domain: auth

## Purpose

Registration, sign-in, identity on every request, and the user profile that drives nutrition
targets. Admin rights and account activation are flags on the same user.

## Key Files

- `apps/api/src/Foodeez.API/Controllers/AuthController.cs` - `POST api/auth/register`,
  `POST api/auth/login`, `GET api/auth/me` (`[Authorize]`).
- `apps/api/src/Foodeez.API/Controllers/UsersController.cs` -
  `GET/PUT api/users/{id:guid}/profile`.
- `apps/api/src/Foodeez.Application/UseCases/Auth/{LoginUseCase,RegisterUseCase}.cs`
- `apps/api/src/Foodeez.Application/UseCases/Users/{GetUserProfileUseCase,UpdateUserProfileUseCase}.cs`
- `apps/api/src/Foodeez.Infrastructure/Services/JwtService.cs`
- `apps/api/src/Foodeez.API/Controllers/{FoodeezController,CurrentUser}.cs`
- `apps/api/src/Foodeez.API/Filters/RequireTokenSubjectFilter.cs`
- `apps/api/src/Foodeez.Domain/Entities/{User,UserProfile}.cs`
- Clients: `apps/web/src/stores/auth.ts`, `apps/web/src/composables/useAuth.ts`,
  `apps/mobile/src/store/authStore.ts`, `apps/mobile/src/services/authBridge.ts`.

## Main Flows

**Register** - `RegisterUseCase` hashes the password with BCrypt, creates the user through
`User.Create` (email lower-cased and trimmed), and returns an `AuthResponse` with a token.

**Login** - look the user up by email, `BCrypt.Verify`, then two refusals, both
`UnauthorizedAccessException` (401): wrong credentials, and a disabled account
("Your account has been disabled..."). `IJwtService` is passed to `ExecuteAsync` as an argument
here rather than injected into the use case.

**Token** - HS256, claims `sub` (user id), `email`, given/surname, `jti`, `iat`, plus a
`ClaimTypes.Role = "Admin"` claim when `User.IsAdmin`. Expiry from `Jwt:ExpiryHours`
(default 24). `Program.cs` validates issuer, audience, lifetime and signing key with
`ClockSkew = TimeSpan.Zero`.

**Identity per request** - `RequireTokenSubjectFilter` runs for every action and 401s an
authenticated request whose token has no readable subject. Actions then read
`FoodeezController.UserId` (a plain `Guid`) or `UserIdOrNull` on endpoints that also serve
anonymous callers. Never take the user id from a route or query parameter.

**Profile** - `UserProfile` holds height, weight, target weight, age, gender, activity level,
dietary goal, per-day macro targets, excluded foods, `ProfileCompleted`, `DarkMode` and
`UnitSystem`. `CalculateAndSetTargets()` derives the calorie and macro targets;
`SetExcludedFoods(...)` owns the exclusion list. Both clients route a user with
`ProfileCompleted == false` to a setup flow.

## Data and State

- Tables: `Users` (email, password hash, names, `IsAdmin`, `IsActive`), `UserProfiles` (1:1).
- Web keeps the token in `localStorage` under `foodeez_token`; the axios response interceptor
  logs out and redirects to `/auth/login` on any 401 except from login/register.
- Mobile keeps the token in `expo-secure-store` (`authStore.ts`).

## External Systems

None. Passwords are hashed locally with `BCrypt.Net-Next`; there is no external identity
provider, no refresh token, and no password reset flow.

## Tests and Validation

- `apps/api/tests/Foodeez.Application.Tests/UseCases/RegisterUseCaseTests.cs`
- `apps/api/tests/Foodeez.Domain.Tests/Entities/{UserProfileTests,UserProfileExclusionTests}.cs`
- `apps/api/tests/Foodeez.Application.Tests/Common/UserProfileMapperTests.cs`

## Known Gotchas

- `Jwt:Key` must be at least 32 characters or startup fails (`RequiredConfiguration`);
  changing it invalidates every issued token.
- `[Authorize]` guarantees a valid signature, not a usable subject claim - that is what the
  filter exists for. Do not reintroduce per-action `if (CurrentUser.IdOf(User) is not ...)`
  checks; under `[Authorize]` that branch is unreachable.
- `AuthResponse.ExpiresAt` is hard-coded to 24 hours in `LoginUseCase` while the token honours
  `Jwt:ExpiryHours` - the two disagree if that setting is changed.
- There is no refresh token; an expired token means signing in again.
