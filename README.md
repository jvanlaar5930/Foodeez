<p align="center">
  <img src="./packages/brand/assets/Foodeez_mark.png" alt="Foodeez icon" width="150" />
</p>

<p align="center">
  <img src="./packages/brand/assets/Foodeez_image.svg" alt="Foodeez" width="420" />
</p>

<p align="center">
  An AI-assisted food journal and meal planner for the web and Android, built on one .NET API.
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img alt="Vue 3" src="https://img.shields.io/badge/Vue-3-4FC08D?style=for-the-badge&logo=vuedotjs&logoColor=white" />
  <img alt="React Native" src="https://img.shields.io/badge/React%20Native-Expo-000020?style=for-the-badge&logo=expo&logoColor=white" />
  <img alt="TypeScript" src="https://img.shields.io/badge/TypeScript-Shared%20Types-3178C6?style=for-the-badge&logo=typescript&logoColor=white" />
  <img alt="MySQL" src="https://img.shields.io/badge/MySQL-8.4-4479A1?style=for-the-badge&logo=mysql&logoColor=white" />
  <img alt="Claude" src="https://img.shields.io/badge/Claude-AI%20Provider-D97757?style=for-the-badge&logo=anthropic&logoColor=white" />
</p>

---

## Summary

**Foodeez** is an AI-assisted food journal and meal planner. Photograph a plate or type a sentence, and it works out what you ate and roughly what was in it. Over days it turns that log into something you can actually read: where your week went, what a pattern looks like, what a sensible next meal would be.

Recipes, a weekly calendar, and a grocery list all hang off the same record, so planning forward and looking back are the same data seen from different ends. It ships as two clients over one API — a Vue 3 web app and a React Native app for Android — with a layered .NET 10 backend that owns the domain and talks to the AI providers.

Claude is the default provider, but Gemini, Groq, OpenRouter, Ollama and LocalAI all sit behind the same interface, so the whole thing can run against a model on your own hardware.

## Philosophy

**Logging has to be cheap, or it doesn't happen.** Every food tracker dies in the same place: the fourth day, when weighing portions and searching a database stops being worth it. So the fastest paths here are a camera and a plain sentence. An approximate log that exists beats a precise one that doesn't.

**The AI reads your food, it doesn't run your life.** A model is used where it is genuinely better than a form — reading a photo, estimating a portion, noticing that three days ran short on protein, drafting a plan you then edit. It doesn't set your goals or grade you. Suggestions arrive as something to accept, change, or ignore.

**Your data is yours, and it's local by default.** Meals you generate stay private to you. The stack runs on your own machine, against your own database and your own API keys, and every AI provider is swappable. Nothing here needs a Foodeez-operated server to work.

**Configuration belongs where you can see it.** Secrets live outside the repo, everything else sits in a file you can diff, and anything worth changing while running is editable from Admin → Settings without a redeploy.

**One brand, one domain, one source of truth.** Shared types, shared artwork, one API. The two clients diverge where the platform demands it and nowhere else.

## Table of Contents

* [Summary](#summary)
* [Philosophy](#philosophy)
* [Features](#features)
* [Layout](#layout)
  * [Backend](#backend)
  * [Clients](#clients)
  * [Shared packages](#shared-packages)
* [AI Providers](#ai-providers)
* [Requirements](#requirements)
* [Quick Start](#quick-start)
* [1. Database Setup (MySQL)](#1-database-setup-mysql)
* [2. Backend API (.NET 10)](#2-backend-api-net-10)
* [3. Web App (Vue 3)](#3-web-app-vue-3)
* [4. Mobile App (React Native / Expo)](#4-mobile-app-react-native--expo)
* [Running Everything Together](#running-everything-together)
* [Testing](#testing)
* [Environment Variables Reference](#environment-variables-reference)
* [Troubleshooting](#troubleshooting)
* [Notes and Limitations](#notes-and-limitations)

## Features

* Log a meal from a photo, a plain description, or a saved template
* AI nutrition estimates streamed back as the model works, editable afterwards
* Recipe library with search, autocomplete, and tag filtering
* A chef's take on any recipe — an AI-enhanced version kept beside the original
* Weekly meal plan calendar, filled by hand or generated
* Plan entries move between days and slots
* Generated meals stay private to the account that made them
* Grocery lists built from a plan and checked off as you shop
* Day-level analysis and recommendations drawn from your actual log
* Conversational chat with your meal history as context
* Six AI providers behind one interface, chosen by configuration
* Provider keys, models, and request timeouts editable at runtime from Admin → Settings
* Application logs viewable in place
* One set of brand artwork and one set of TypeScript types shared by both clients

## Layout

```text
Foodeez/
├─ apps/
│  ├─ api/          .NET 10 backend — REST API + AI integration
│  ├─ mobile/       React Native (Expo) — Android-first mobile app
│  └─ web/          Vue 3 — browser web app
├─ packages/
│  ├─ brand/        Logo and icon artwork shared by both clients
│  └─ shared/       Shared TypeScript types used by mobile and web
├─ docker-compose.yml
└─ start-all.bat
```

### Backend

The API is layered, one project per concern:

```text
Foodeez.Domain           entities — meals, plans, recipes, grocery lists, users
Foodeez.Application      use cases
Foodeez.Infrastructure   EF Core persistence and provider clients
Foodeez.API              controllers
```

Tests sit alongside as `Foodeez.Domain.Tests`, `Foodeez.Application.Tests`, and
`Foodeez.Integration.Tests`.

### Clients

Both clients talk to the same REST API and render the same domain. The web app proxies `/api`
to the backend in development; the mobile app derives the API host from the address its bundle
arrived from, so it usually needs no configuration at all.

### Shared packages

`@foodeez/shared` holds the TypeScript types both clients use. `@foodeez/brand` holds the logo
and icon artwork, resolved straight to source files through a bundler alias rather than a build
step, so a logo change lands in both clients at once.

## AI Providers

Claude is the default. Every provider sits behind the same interface and is chosen by
configuration rather than by code:

```text
Claude       default, vision-capable
Gemini       alternative hosted provider
Groq         alternative hosted provider, fast
OpenRouter   one key for many models, free routing supported
Ollama       self-hosted, local
LocalAI      self-hosted, local
```

Each carries its own request timeout. A self-hosted model can spend minutes on one prompt, and
the timeout is what decides whether that is patience or a failure — which is why `Ollama` and
`LocalAI` default to 300 seconds while `Groq` defaults to 60.

---

## Requirements

| Tool | Version | Download |
|------|---------|----------|
| Node.js | 20 LTS or higher | https://nodejs.org |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| MySQL | 8.4 | https://dev.mysql.com/downloads/installer (Windows) |
| Docker Desktop *(optional, MySQL alternative)* | Latest | https://www.docker.com/products/docker-desktop |
| Android Studio *(mobile only)* | Latest | https://developer.android.com/studio |
| Expo Go app *(mobile, physical device)* | Latest | Play Store |

Verify installs:

```powershell
node --version
dotnet --version
mysql --version
```

## Quick Start

With MySQL already running and secrets configured, three terminals from the repo root:

```powershell
dotnet run --project apps/api/src/Foodeez.API
npm install; npm run shared:build; npm run web
npm run android
```

The sections below cover each of those in full.

## 1. Database Setup (MySQL)

You need a running MySQL 8.4 instance. Choose one of the two options below.

### Option A — Docker (recommended if Docker Desktop is running)

```powershell
docker run -d --name foodeez-mysql -e MYSQL_ROOT_PASSWORD=foodeez123 -e MYSQL_DATABASE=foodeez -p 3306:3306 mysql:8.4
```

Stop / start without losing data:

```powershell
docker stop foodeez-mysql
docker start foodeez-mysql
```

### Option B — MySQL Installer (Windows)

1. Download the MySQL Installer from https://dev.mysql.com/downloads/installer
2. Run the installer and choose **Server Only** or **Developer Default**
3. Set the root password to `foodeez123` (or any password — update `appsettings.json` to match)
4. Leave the port as **3306**
5. Complete the installer — MySQL runs as a Windows service automatically

After installing, create the database:

```powershell
mysql -u root -p
```

```sql
CREATE DATABASE foodeez;
EXIT;
```

## 2. Backend API (.NET 10)

All commands below are run from the `apps/api/` directory.

### Step 1 — Configure secrets (first time only)

`appsettings.json` is committed, so **no secret goes in it**. Locally they live in .NET's
user-secrets store, which sits outside the repo and cannot be committed by accident.

```powershell
cd apps/api/src/Foodeez.API

dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=foodeez;User=root;Password=foodeez123;Port=3306"
dotnet user-secrets set "Jwt:Key" "any-random-string-that-is-at-least-32-characters-long"
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
```

Optional, only if you use them — `Spoonacular:ApiKey`, `FoodData:ApiKey`, `Gemini:ApiKey`,
`Groq:ApiKey`. `dotnet user-secrets list` shows what is set.

* **JWT Key** — any random string, minimum 32 characters. Changing it signs everyone out.
* **Claude API Key** — get one at https://console.anthropic.com

Everything that is *not* a secret — model names, Ollama/LocalAI URLs, token ceilings, log
levels — stays in `appsettings.json`, where it is reviewable in diffs.

> **Admin > Settings overrides all of it at runtime.** Every provider's key, model, and
> request timeout can be edited there without a redeploy; what is in `appsettings.json` is the
> starting point a fresh installation uses until a row is saved. Timeouts are worth knowing
> about: a self-hosted model can spend minutes on one prompt, and the timeout is what decides
> whether that is patience or a failure.

> **Environments.** User-secrets are loaded only in Development. `launchSettings.json` and
> `start-all.bat` both set `ASPNETCORE_ENVIRONMENT=Development`, which is also what enables
> Swagger. Deployed environments get their configuration from environment variables instead
> (`Jwt__Key`, `Claude__ApiKey`, ... — `:` becomes `__`); `docker-compose.yml` reads those
> from a `.env` file, and `.env.example` is the template.
>
> The API refuses to start if `ConnectionStrings:Default`, `Jwt:Key`, `Jwt:Issuer` or
> `Jwt:Audience` is missing, and names the offender — rather than failing later on someone's
> first login.

### Step 2 — Restore packages

```powershell
cd apps/api
dotnet restore
```

### Step 3 — Apply database migrations

```powershell
dotnet ef migrations add InitialCreate --project src/Foodeez.Infrastructure --startup-project src/Foodeez.API
dotnet ef database update --startup-project src/Foodeez.API
```

### Step 4 — Run the API

```powershell
dotnet run --project src/Foodeez.API
```

```text
API           http://localhost:5000
Swagger UI    http://localhost:5000/swagger
Health check  http://localhost:5000/health
```

## 3. Web App (Vue 3)

### Step 1 — Install dependencies

From the **repo root** (`c:\dev\Foodeez`):

```powershell
npm install
```

This installs packages for all workspaces (web, mobile, shared) at once.

### Step 2 — Build the shared types package

```powershell
npm run shared:build
```

### Step 3 — Run the web app

```powershell
npm run web
```

Opens at **http://localhost:3000**

The web app proxies all `/api` requests to `http://localhost:5000`, so the backend must be
running for login, data fetching, etc.

### Build for production

```powershell
npm run web:build
```

Output goes to `apps/web/dist/`.

## 4. Mobile App (React Native / Expo)

### Step 1 — Install dependencies

From the repo root (if not already done):

```powershell
npm install
```

### Step 2 — Configure the API URL (usually not needed)

The app works this out for itself: on an emulator it uses the emulator's alias for your host,
and on a physical device it takes the host out of the address the bundle arrived from. Set it
by hand only to point somewhere else — a staging API, or a tunnelled dev server — by creating
`apps/mobile/.env` with:

```text
EXPO_PUBLIC_API_URL=http://192.168.1.50:5000/api
```

> The `/api` suffix is part of the value: it is the base every request is appended to, so
> without it every call 404s and login fails.
> `10.0.2.2` is the Android emulator's address for `localhost` on your host machine; a
> physical device needs your machine's local IP on the same Wi-Fi.

### Step 3 — Start the app

**Android emulator** (requires Android Studio with a virtual device set up):

```powershell
npm run android
```

**Physical Android device** (requires Expo Go installed from Play Store):

```powershell
npm run mobile
```

Then scan the QR code with the Expo Go app.

### Step 4 — Android emulator setup (first time only)

1. Open **Android Studio**
2. Go to **Virtual Device Manager** (Device Manager icon in the toolbar)
3. Click **Create Device** → choose a phone (e.g. Pixel 8) → select a system image (API 34 recommended) → Finish
4. Click the play button to launch the emulator
5. Run `npm run android` — Expo will detect the emulator automatically

### Build a standalone APK

An `.apk` you can sideload onto an Android phone — no Expo Go, no dev server, and it talks to the
deployed API rather than your machine. The build profiles live in `apps/mobile/eas.json`.

**Option A — EAS cloud build (recommended, nothing to install locally)**

```powershell
cd apps/mobile
npx eas-cli login          # first time only
npx eas-cli build --platform android --profile preview
```

The `preview` profile sets `"buildType": "apk"` and points `EXPO_PUBLIC_API_URL` at the deployed
API, so the build needs nothing else from you. On the first run EAS offers to generate an Android
keystore — accept, and it reuses that keystore for every later build.

The build queues on Expo's servers and takes roughly 10–20 minutes. When it finishes the CLI prints
a URL and a QR code: open that link in the browser on your phone, download the APK, and tap it.
Android asks you to allow installs from that browser the first time.

> Use `--profile preview`, not `--profile production`. The production profile sets no `buildType`,
> so it emits an `.aab` for the Play Store — that format cannot be sideloaded.

> The `projectId` under `extra.eas` in `app.json` ties the build to one Expo account. If EAS reports
> `Entity not authorized` for that id, you are logged in as a different account than the one that
> owns the project — check with `npx eas-cli whoami`.

**Option B — Local build (no queue, but needs the Android toolchain)**

Requires JDK 17 or later and the Android SDK on your machine:

```powershell
cd apps/mobile
npx expo prebuild --platform android
cd android
./gradlew assembleRelease
```

The APK lands at `apps/mobile/android/app/build/outputs/apk/release/app-release.apk`. Copy it to
your phone over USB or cloud storage and tap it to install.

> `expo prebuild` generates an `apps/mobile/android/` directory and rewrites the `android` and `ios`
> scripts in `apps/mobile/package.json` to `expo run:*`. Both are derived from `app.json` and safe to
> regenerate, so revert the `package.json` change and keep the `android/` directory out of commits.

## Running Everything Together

Open three terminal windows and run these in order:

| Terminal | Directory | Command |
|----------|-----------|---------|
| 1 — API | `apps/api` | `dotnet run --project src/Foodeez.API` |
| 2 — Web | repo root | `npm run web` |
| 3 — Mobile | repo root | `npm run android` |

## Testing

```powershell
cd apps/api
dotnet test
```

Run a specific test project:

```powershell
dotnet test tests/Foodeez.Domain.Tests
dotnet test tests/Foodeez.Application.Tests
dotnet test tests/Foodeez.Integration.Tests
```

Run with detailed output:

```powershell
dotnet test --verbosity normal
```

## Environment Variables Reference

### Backend

Secrets come from user-secrets in Development and environment variables when deployed
(`:` becomes `__`, so `Jwt:Key` is `Jwt__Key`). Non-secrets live in `appsettings.json`.

| Key | Secret? | Description | Example |
|-----|---------|-------------|---------|
| `ConnectionStrings:Default` | yes | MySQL connection string | `Server=localhost;Database=foodeez;User=root;Password=...;Port=3306` |
| `Jwt:Key` | yes | JWT signing secret (min 32 chars) | a long random string |
| `Jwt:Issuer` / `Jwt:Audience` | no | Token issuer and audience | `Foodeez` / `FoodeezApp` |
| `Jwt:ExpiryHours` | no | How long tokens stay valid | `24` |
| `Claude:ApiKey` | yes | Anthropic API key | `sk-ant-api03-...` |
| `Claude:Model` | no | Claude model to use | `claude-sonnet-4-6` |
| `<Provider>:TimeoutSeconds` | no | How long one call to that provider may run before it is abandoned and the feature falls back. Every provider has one: `Claude` and `Gemini` 120, `Groq` 60, `Ollama` and `LocalAI` 300. | `120` |
| `Spoonacular:ApiKey` | yes | Recipe search | |
| `FoodData:ApiKey` | yes | USDA food search | `DEMO_KEY` works for light use |
| `Gemini:ApiKey` / `Groq:ApiKey` | yes | Alternative AI providers | |
| `OpenRouter:ApiKey` | yes | One key for many providers — a free key still works, see below | `sk-or-v1-...` |
| `OpenRouter:UseFreeModels` | no | Route through `openrouter/free`, which picks a zero-cost model per request and bills nothing. On unless set false, in which case `OpenRouter:Model` must name a slug from openrouter.ai/models. | `true` |
| `Ollama:*` / `LocalAI:*` | no | Self-hosted provider URL and model | see `appsettings.json` |
| `Cors:AllowedOrigins` | no | Browser origins allowed outside Development. Empty is correct when the bundled nginx serves the web app, since that is same-origin. | `[]` |

### Mobile — `apps/mobile/.env`

| Key | Description | Example |
|-----|-------------|---------|
| `EXPO_PUBLIC_API_URL` | Backend API base URL, including `/api`. Optional — the app derives it from the dev server address otherwise. | `http://10.0.2.2:5000/api` (emulator) or `http://192.168.x.x:5000/api` (physical device) |

## Troubleshooting

### `docker: error during connect` when running the Docker MySQL command

Docker Desktop is not running. Open Docker Desktop from the Start menu and wait for the whale
icon in the system tray to stop animating, then retry.

### `dotnet ef` command not found

Install the EF Core CLI tools:

```powershell
dotnet tool install --global dotnet-ef
```

### MySQL connection refused

* Confirm MySQL is running: `docker ps` (Docker) or check Services in Task Manager (native install)
* Confirm the password in your `ConnectionStrings:Default` user-secret matches what you set during install (`dotnet user-secrets list`)
* Confirm port 3306 is not blocked by a firewall

### Expo Go: `java.io.IOException: failed to download remote update`

Expo Go could not fetch the JS bundle from Metro on your machine — this happens before any
screen renders, so it is not a login failure even when it looks like one. In order of
likelihood:

1. **An older Metro is still holding port 8081.** A dev server left over from a previous
   session answers the QR code but may no longer be serving a usable bundle. Find and stop it,
   then start a fresh one:
   ```powershell
   Get-NetTCPConnection -LocalPort 8081 -State Listen | Select-Object OwningProcess
   Stop-Process -Id <that pid>
   npm run mobile
   ```
2. **Windows Firewall is blocking node on the private network.** Run once, in an elevated
   PowerShell:
   ```powershell
   New-NetFirewallRule -DisplayName "Expo Metro 8081" -Direction Inbound -Protocol TCP -LocalPort 8081 -Action Allow -Profile Private
   ```
3. **The phone is not on the same network**, or the Wi-Fi has client isolation on (common on
   guest networks). Check that the IP in the Metro banner matches your `ipconfig` IPv4 address,
   and that the phone can open `http://<that IP>:8081` in its browser.
4. **Neither is fixable right now** — go over Expo's relay, which needs no LAN path:
   ```powershell
   npm run mobile:tunnel
   ```
   A tunnelled dev server is not an address the API is reachable on, so set
   `EXPO_PUBLIC_API_URL=http://192.168.x.x:5000/api` in `apps/mobile/.env` for that session.
5. **A stale bundler cache**, after a dependency change: `npm run mobile -- --clear`.

### Expo / Metro bundler can't connect to API on physical device

Use your machine's local IP address instead of `10.0.2.2`:

```powershell
ipconfig   # find your IPv4 address under Wi-Fi adapter
```

Then set `EXPO_PUBLIC_API_URL=http://192.168.x.x:5000/api` in `apps/mobile/.env`.

### `npm install` fails with workspace errors

Make sure you are running `npm install` from the **repo root** (`c:\dev\Foodeez`), not inside a
sub-folder.

### Android emulator is very slow

Enable hardware acceleration in Android Studio:

* Go to **SDK Manager → SDK Tools → Install Android Emulator Hypervisor Driver**
* Or enable HAXM / Hyper-V in your BIOS settings

### Port 5000 already in use

Change the API port in `apps/api/src/Foodeez.API/Properties/launchSettings.json`, then update
`API_PORT` in `apps/mobile/src/constants/api.ts` (and `EXPO_PUBLIC_API_URL`, if you set one) and
the Vite proxy in `apps/web/vite.config.ts` accordingly.

## Notes and Limitations

Nutrition figures from a photo or a description are estimates, not measurements. They are useful
for spotting patterns across a week; they are not a substitute for weighing food when precision
actually matters.

Provider behaviour varies. Vision-based meal analysis needs a vision-capable model, so a small
self-hosted model configured through Ollama or LocalAI may handle text logging well and photo
logging poorly. Request timeouts exist for the same reason — set them to match the hardware you
are pointing at.

The mobile app is Android-first. The Expo project can build for iOS, but nothing here is tested
against it.
