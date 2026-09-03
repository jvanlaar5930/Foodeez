# Foodeez

An AI-powered food tracking and meal prepping application. Log meals, scan food with your camera, get personalized nutrition insights from Claude AI, and plan your weekly meals with an interactive calendar.

---

## Table of Contents

- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [1. Database Setup (MySQL)](#1-database-setup-mysql)
- [2. Backend API (.NET 10)](#2-backend-api-net-10)
- [3. Web App (Vue 3)](#3-web-app-vue-3)
- [4. Mobile App (React Native / Expo)](#4-mobile-app-react-native--expo)
- [Running Everything Together](#running-everything-together)
- [Testing](#testing)
- [Environment Variables Reference](#environment-variables-reference)
- [Troubleshooting](#troubleshooting)

---

## Project Structure

```
Foodeez/
├── apps/
│   ├── api/          # .NET 10 backend — REST API + AI integration
│   ├── mobile/       # React Native (Expo) — Android-first mobile app
│   └── web/          # Vue 3 — browser web app
└── packages/
    └── shared/       # Shared TypeScript types used by mobile and web
```

---

## Prerequisites

Install the following before getting started:

| Tool | Version | Download |
|------|---------|----------|
| Node.js | 20 LTS or higher | https://nodejs.org |
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| MySQL | 8.4 | https://dev.mysql.com/downloads/installer (Windows) |
| Docker Desktop *(optional, MySQL alternative)* | Latest | https://www.docker.com/products/docker-desktop |
| Android Studio *(mobile only)* | Latest | https://developer.android.com/studio |
| Expo Go app *(mobile, physical device)* | Latest | Play Store |

Verify installs:

```
node --version
dotnet --version
mysql --version
```

---

## 1. Database Setup (MySQL)

You need a running MySQL 8.4 instance. Choose one of the two options below.

### Option A — Docker (recommended if Docker Desktop is running)

```
docker run -d --name foodeez-mysql -e MYSQL_ROOT_PASSWORD=foodeez123 -e MYSQL_DATABASE=foodeez -p 3306:3306 mysql:8.4
```

Stop / start without losing data:
```
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
```
mysql -u root -p
```
```sql
CREATE DATABASE foodeez;
EXIT;
```

---

## 2. Backend API (.NET 10)

All commands below are run from the `apps/api/` directory.

### Step 1 — Configure secrets (first time only)

`appsettings.json` is committed, so **no secret goes in it**. Locally they live in .NET's
user-secrets store, which sits outside the repo and cannot be committed by accident.

```
cd apps/api/src/Foodeez.API

dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;Database=foodeez;User=root;Password=foodeez123;Port=3306"
dotnet user-secrets set "Jwt:Key" "any-random-string-that-is-at-least-32-characters-long"
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
```

Optional, only if you use them — `Spoonacular:ApiKey`, `FoodData:ApiKey`, `Gemini:ApiKey`,
`Groq:ApiKey`. `dotnet user-secrets list` shows what is set.

- **JWT Key** — any random string, minimum 32 characters. Changing it signs everyone out.
- **Claude API Key** — get one at https://console.anthropic.com

Everything that is *not* a secret — model names, Ollama/LocalAI URLs, token ceilings, log
levels — stays in `appsettings.json`, where it is reviewable in diffs.

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

```
cd apps/api
dotnet restore
```

### Step 3 — Apply database migrations

```
dotnet ef migrations add InitialCreate --project src/Foodeez.Infrastructure --startup-project src/Foodeez.API
dotnet ef database update --startup-project src/Foodeez.API
```

### Step 4 — Run the API

```
dotnet run --project src/Foodeez.API
```

The API starts at **http://localhost:5000**

Swagger UI (interactive API docs): **http://localhost:5000/swagger**

Health check endpoint: **http://localhost:5000/health**

---

## 3. Web App (Vue 3)

### Step 1 — Install dependencies

From the **repo root** (`c:\dev\Foodeez`):

```
npm install
```

This installs packages for all workspaces (web, mobile, shared) at once.

### Step 2 — Build the shared types package

```
npm run shared:build
```

### Step 3 — Run the web app

```
npm run web
```

Opens at **http://localhost:3000**

The web app proxies all `/api` requests to `http://localhost:5000`, so the backend must be running for login, data fetching, etc.

### Build for production

```
npm run web:build
```

Output goes to `apps/web/dist/`.

---

## 4. Mobile App (React Native / Expo)

### Step 1 — Install dependencies

From the repo root (if not already done):

```
npm install
```

### Step 2 — Configure the API URL

Create `apps/mobile/.env` with:

```
EXPO_PUBLIC_API_URL=http://10.0.2.2:5000
```

> `10.0.2.2` is the Android emulator's address for `localhost` on your host machine.
> For a physical device on the same Wi-Fi, replace with your machine's local IP (e.g. `http://192.168.1.50:5000`).

### Step 3 — Start the app

**Android emulator** (requires Android Studio with a virtual device set up):

```
npm run android
```

**Physical Android device** (requires Expo Go installed from Play Store):

```
npm run mobile
```

Then scan the QR code with the Expo Go app.

### Step 4 — Android emulator setup (first time only)

1. Open **Android Studio**
2. Go to **Virtual Device Manager** (Device Manager icon in the toolbar)
3. Click **Create Device** → choose a phone (e.g. Pixel 8) → select a system image (API 34 recommended) → Finish
4. Click the play button to launch the emulator
5. Run `npm run android` — Expo will detect the emulator automatically

---

## Running Everything Together

Open three terminal windows and run these in order:

| Terminal | Directory | Command |
|----------|-----------|---------|
| 1 — API | `apps/api` | `dotnet run --project src/Foodeez.API` |
| 2 — Web | repo root | `npm run web` |
| 3 — Mobile | repo root | `npm run android` |

---

## Testing

### Backend tests

```
cd apps/api
dotnet test
```

Run a specific test project:

```
dotnet test tests/Foodeez.Domain.Tests
dotnet test tests/Foodeez.Application.Tests
dotnet test tests/Foodeez.Integration.Tests
```

Run with detailed output:

```
dotnet test --verbosity normal
```

---

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
| `Spoonacular:ApiKey` | yes | Recipe search | |
| `FoodData:ApiKey` | yes | USDA food search | `DEMO_KEY` works for light use |
| `Gemini:ApiKey` / `Groq:ApiKey` | yes | Alternative AI providers | |
| `Ollama:*` / `LocalAI:*` | no | Self-hosted provider URL and model | see `appsettings.json` |
| `Cors:AllowedOrigins` | no | Browser origins allowed outside Development. Empty is correct when the bundled nginx serves the web app, since that is same-origin. | `[]` |

### Mobile — `apps/mobile/.env`

| Key | Description | Example |
|-----|-------------|---------|
| `EXPO_PUBLIC_API_URL` | Backend API base URL | `http://10.0.2.2:5000` (emulator) or `http://192.168.x.x:5000` (physical device) |

---

## Troubleshooting

### `docker: error during connect` when running the Docker MySQL command
Docker Desktop is not running. Open Docker Desktop from the Start menu and wait for the whale icon in the system tray to stop animating, then retry.

### `dotnet ef` command not found
Install the EF Core CLI tools:
```
dotnet tool install --global dotnet-ef
```

### MySQL connection refused
- Confirm MySQL is running: `docker ps` (Docker) or check Services in Task Manager (native install)
- Confirm the password in your `ConnectionStrings:Default` user-secret matches what you set during install (`dotnet user-secrets list`)
- Confirm port 3306 is not blocked by a firewall

### Expo / Metro bundler can't connect to API on physical device
Use your machine's local IP address instead of `10.0.2.2`:
```
ipconfig   # find your IPv4 address under Wi-Fi adapter
```
Then set `EXPO_PUBLIC_API_URL=http://192.168.x.x:5000` in `apps/mobile/.env`.

### `npm install` fails with workspace errors
Make sure you are running `npm install` from the **repo root** (`c:\dev\Foodeez`), not inside a sub-folder.

### Android emulator is very slow
Enable hardware acceleration in Android Studio:
- Go to **SDK Manager → SDK Tools → Install Android Emulator Hypervisor Driver**
- Or enable HAXM / Hyper-V in your BIOS settings

### Port 5000 already in use
Change the API port in `apps/api/src/Foodeez.API/Properties/launchSettings.json`, then update `EXPO_PUBLIC_API_URL` and the Vite proxy in `apps/web/vite.config.ts` accordingly.
