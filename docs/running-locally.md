# Running Nurtricenter (ms5) locally — step by step

This guide brings up ms5 on your machine, together with the rest of the Nur-Tricenter platform:
gateway, Consul, RabbitMQ, catalog (ms2), identity and delivery calendar (ms4). Follow it top to
bottom the first time. After that, only [§4 Start the stack](#4-start-the-stack) is needed.

For *how* the pieces connect (outbox, Consul registration, gateway routing), see
[connectivity-and-messaging.md](connectivity-and-messaging.md).

> All commands are **Windows PowerShell**, run from any folder unless a step says otherwise.
> `pwsh` (PowerShell 7) is **not** required: every script is run with
> `powershell -ExecutionPolicy Bypass -File ...`.

---

## Contents

1. [What you will run](#1-what-you-will-run)
2. [Prerequisites](#2-prerequisites)
3. [One-time setup](#3-one-time-setup)
4. [Start the stack](#4-start-the-stack)
5. [Verify everything works](#5-verify-everything-works)
6. [Day-to-day workflow](#6-day-to-day-workflow)
7. [Stop the stack](#7-stop-the-stack)
8. [Other ways to run ms5](#8-other-ways-to-run-ms5)
9. [Troubleshooting](#9-troubleshooting)
10. [Reference: ports, URLs, names](#10-reference-ports-urls-names)

---

## 1. What you will run

```
                    http://localhost:15005
 you ──────────────► nur-tricenter-gateway (YARP) ──discovers routes in──► Consul (:8500)
                          │
                          ├─ /api/catalog/...         → catalog-api          (ms2)
                          ├─ /api/identity-service/... → identity-api
                          ├─ /api/delivery-calendar/...→ delivery-calendar-api (ms4)
                          └─ /api/nurtricenter/...     → nurtricenter-api     (ms5)  ──► Neon (cloud Postgres)
                                                                                          ▲
                                         nurtricenter-workerservice (ms5 outbox) ─────────┘
                                                    │ publishes events
                                                    ▼
                                             RabbitMQ (:5672, UI :15672)
```

- **The platform** (`nur-tricenter-local`) is a Docker Compose stack. It runs the shared
  infrastructure (Postgres, RabbitMQ, Consul), the gateway, and every microservice.
- **Every microservice runs from its own repo's `bin/Debug` build output**, bind-mounted into a
  plain `aspnet` container. Rebuild a project and restart its container to pick up changes.
- **ms5 uses its own Neon database**, not the platform Postgres. The other services use the
  platform Postgres (host port `5433`).
- ms5 is two processes. `nurtricenter-api` serves HTTP and registers itself in Consul.
  `nurtricenter-workerservice` has no HTTP; it drains the outbox and publishes to RabbitMQ.

---

## 2. Prerequisites

| Need | Why | Check |
|---|---|---|
| **Docker Desktop**, running | the whole stack runs in containers | `docker version` shows a *Server* section |
| **.NET SDK 10** | ms5 and the gateway target `net10.0` | `dotnet --list-sdks` shows `10.x` |
| **.NET SDK 8** | catalog and identity target `net8.0` | `dotnet --list-sdks` shows `8.x` |
| **git** | `setup-project` clones the sibling repos | `git --version` |
| **GitHub classic token** with `read:packages` | the shared kernels (`Nur.Tricenter.*`) come from the private Nur-tricenter feed; catalog, identity, ms4 **and ms5** need them to build | see §3.1 |
| **ms5's Neon connection string** | ms5's database | ask the ms5 owner; never commit it |
| Free host ports **5433, 5672, 15672, 8500, 15005** | published by the platform containers | stop anything else using them, e.g. old `consul-dev` / `rabbitmq-dev` containers |

---

## 3. One-time setup

### 3.1 Store the GitHub token for the private NuGet feed

Create a **classic** personal access token on GitHub (Settings → Developer settings → Personal
access tokens → Tokens (classic)) with the `read:packages` scope. Then:

```powershell
[Environment]::SetEnvironmentVariable("NUGET_USERNAME", "<your-github-username>", "User")
[Environment]::SetEnvironmentVariable("NUGET_TOKEN",    "<your-token>",           "User")
```

**Close and reopen every terminal and IDE** afterwards. Processes that are already running never
see new user variables.

The token is only ever read from these variables. Every repo's `NuGet.config` references
`%NUGET_TOKEN%`; never paste the token into a file.

### 3.2 Lay out the workspace

All repos must be **siblings** in one folder, because the platform compose finds them by
relative path:

```
C:\Dev\dotnet projects\NUR\FinalProject\
├── nur-tricenter-local\                 ← the platform (compose + scripts)
├── nur-tricenter-api-gateway\
├── nur-tricenter-shared-kernels\
├── nur-tricenter-identity\
├── ms2-plans-and-recipes-catalog\
├── ms4-delivery-schedule-management\
└── modulo-2-actividad-3-DruGuerrero\    ← ms5 (on GitHub: nurtricenter-logistics-and-delivery)
```

Clone the platform repo, then let its script clone the rest:

```powershell
cd "C:\Dev\dotnet projects\NUR\FinalProject"
git clone https://github.com/Nur-tricenter/nur-tricenter-local.git
powershell -ExecutionPolicy Bypass -File ".\nur-tricenter-local\scripts\setup-project.ps1"
```

The script is idempotent. Repos you already have are left alone (`ya existe`), missing ones are
cloned (`clonando` → `listo`). Re-run it whenever the platform adds a service. If a repo it
expects is missing, **every** `docker compose` command for the platform fails, because the
compose `include:` can't find that repo's file.

### 3.3 Tell the platform where ms5 lives

The platform looks for ms5 in a folder named `nurtricenter-logistics-and-delivery`, which is the
GitHub repo name. If your ms5 folder has another name, set `NURTRICENTER_REPO_DIR` once,
permanently:

```powershell
[Environment]::SetEnvironmentVariable("NURTRICENTER_REPO_DIR", "modulo-2-actividad-3-DruGuerrero", "User")
```

Reopen your terminals afterwards. Without this, compose fails with
`open ...\nurtricenter-logistics-and-delivery\nurtricenter.compose.yml: The system cannot find the file specified`.

> If `setup-project` cloned a second copy of ms5 as `nurtricenter-logistics-and-delivery`, either
> work in that copy (and skip this step), or delete it and keep `NURTRICENTER_REPO_DIR`.

### 3.4 Give ms5 its database connection string

In the **ms5 repo root**, copy `.env.example` to `.env` and put the Neon connection string in it:

```powershell
cd "C:\Dev\dotnet projects\NUR\FinalProject\modulo-2-actividad-3-DruGuerrero"
Copy-Item .env.example .env
notepad .env
```

```
ConnectionStrings__DefaultConnection=Host=<...>.neon.tech;Database=neondb;Username=<...>;Password=<...>;SSL Mode=Require
```

- One line, `KEY=value`, **no quotes**.
- `.env` is gitignored. Before every commit, `git status` must not list it.
- Without `.env`, both ms5 containers still start, but they fail on their first database query.

### 3.5 Make sure the Neon schema is up to date

The platform never migrates ms5's database. If ms5 has migrations that haven't been applied to
Neon yet, apply them by hand, from `src/Nurtricenter.Infrastructure`:

```powershell
cd "C:\Dev\dotnet projects\NUR\FinalProject\modulo-2-actividad-3-DruGuerrero\src\Nurtricenter.Infrastructure"
dotnet ef database update --startup-project ../Nurtricenter.Api
```

This reads the connection string from `src/Nurtricenter.Api/appsettings.Development.json`, which
is gitignored. Running it when nothing is pending is a harmless no-op.

---

## 4. Start the stack

1. Start **Docker Desktop** and wait until it says *Engine running*.
2. In PowerShell:

   ```powershell
   powershell -ExecutionPolicy Bypass -File "C:\Dev\dotnet projects\NUR\FinalProject\nur-tricenter-local\scripts\start-stack.ps1"
   ```

   (If you skipped §3.3's permanent variable, run
   `$env:NURTRICENTER_REPO_DIR = 'modulo-2-actividad-3-DruGuerrero'` first, **in the same window**.)

3. Wait for `Stack ready.` The first run takes a few minutes. The script:
   1. removes old `nur-tricenter-*` containers (volumes and data are kept);
   2. creates the `nur-tricenter-net` Docker network if it's missing;
   3. `dotnet build`s catalog, identity, ms4, **ms5 (API + worker)** and the gateway;
   4. starts everything with `docker compose up -d`;
   5. applies the catalog schema;
   6. logs in to identity as `admin` and creates ms4's service account. This can wait up to ~2 min
      while the gateway learns identity's route from Consul.

---

## 5. Verify everything works

### 5.1 Containers

```powershell
docker ps -a --format "table {{.Names}}\t{{.Status}}"
```

- Every `nur-tricenter-*` container shows **`(healthy)`**.
- The two `-migrate` containers show **`Exited (0)`**. They run once and stop; that's success.
- A container that keeps restarting is usually a stale or failed build. Rebuild that project
  (§6) and check its logs (§9).

### 5.2 Consul

Open <http://localhost:8500>. The services `catalog`, `identity-service`, `delivery-calendar` and
**`nurtricenter`** are listed, each with green checks. The gateway only routes to services that
are registered here and passing.

### 5.3 ms5 through the gateway (no login needed)

```powershell
Invoke-RestMethod -Method Post -Uri http://localhost:15005/api/nurtricenter/v1/couriers `
  -ContentType 'application/json' -Body '{"fullName":"Jane Doe"}'
```

This returns `201` with the new courier's id. It writes a real courier to Neon.

> The gateway rewrites `/api/nurtricenter/v1/couriers` → `/api/v1/couriers` inside the ms5
> container. That URL is **POST-only**. Opening it in a browser sends a GET and returns
> `405 Method Not Allowed`. That `405` comes from ms5 itself, so it still proves the route works.

### 5.4 Event delivery to RabbitMQ

Open <http://localhost:15672> (guest / guest). Within about 5 seconds of §5.3:

- **Exchanges** → `courier-created` shows a message rate.
- **Queues** → `nurtricenter.default` shows **Ready** going up by 1. Click it → *Get messages* to see
  the payload: `EventType: "Courier.Created"`, `Source: "nurtricenter"`, `CorrelationId`, and the
  courier data.

`nurtricenter.default` is a history sink that nothing consumes, so its count only grows. Other
services read ms5 events from their own queues.

### 5.5 Another service through the gateway (login needed)

Catalog, identity and ms4 require a JWT from identity, so a browser gets `401`. Use PowerShell:

```powershell
$token = (Invoke-RestMethod -Method Post -Uri http://localhost:15005/api/identity-service/v1/auth/login `
  -ContentType 'application/json' -Body '{"userName":"admin","password":"admin"}').accessToken

Invoke-RestMethod http://localhost:15005/api/catalog/v1/foods -Headers @{ Authorization = "Bearer $token" } |
  ConvertTo-Json -Depth 5
```

`admin` / `admin` is the local-dev administrator that identity seeds. Tokens last 1 hour; on a
`401`, run the first command again.

---

## 6. Day-to-day workflow

**After changing ms5 code**, rebuild, then restart its container:

```powershell
cd "C:\Dev\dotnet projects\NUR\FinalProject\modulo-2-actividad-3-DruGuerrero"
dotnet build src/Nurtricenter.Api/Nurtricenter.Api.csproj
dotnet build src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj
docker restart nur-tricenter-nurtricenter-api nur-tricenter-nurtricenter-workerservice
```

**After changing `.env` or `nurtricenter.compose.yml`**, recreate the containers (a restart doesn't
re-read environment settings):

```powershell
$c = "C:\Dev\dotnet projects\NUR\FinalProject\nur-tricenter-local\compose"
docker compose -f "$c\docker-compose.yml" -f "$c\docker-compose.infra.yml" -p nur-tricenter up -d --force-recreate nurtricenter-api nurtricenter-workerservice
```

**Logs:**

```powershell
docker logs -f nur-tricenter-nurtricenter-api
docker logs -f nur-tricenter-nurtricenter-workerservice
docker logs -f nur-tricenter-gateway
```

**Tests** (they never touch Consul, RabbitMQ or Neon):

```powershell
dotnet test src/Nurtricenter.Api/Nurtricenter.Api.slnx
```

**After pulling other repos**, re-run `setup-project.ps1` (new services) and `start-stack.ps1`.

---

## 7. Stop the stack

```powershell
docker compose -p nur-tricenter stop      # stop everything, keep containers and data
docker compose -p nur-tricenter start     # start them again later, no rebuild
```

`start-stack.ps1` always works too: it removes and recreates the containers. Data survives in the
`nur-tricenter-postgres-data` volume, and ms5's data lives in Neon.

To wipe the platform database (catalog, identity and ms4 data), run
`docker volume rm nur-tricenter-postgres-data` after removing the containers. This never touches
ms5's Neon data.

---

## 8. Other ways to run ms5

### 8.1 API and worker from your IDE (debugging), everything else in Docker

1. Start the stack (§4), then stop the two ms5 containers so they don't compete with your local
   processes:

   ```powershell
   docker stop nur-tricenter-nurtricenter-api nur-tricenter-nurtricenter-workerservice
   ```

2. Configure the gitignored `src/Nurtricenter.Api/appsettings.Development.json`:
   - `ConnectionStrings:DefaultConnection`: Neon.
   - `ServiceRegistration:ServiceDiscoveryAddress`: `http://localhost:8500`.
   - `ServiceRegistration:ServiceName`: `nurtricenter`. This is the gateway path segment; with
     another name the route becomes `/api/<that-name>/...`.
   - `ServiceRegistration:Host`: **`host.docker.internal`**. Consul runs in a container and must
     reach your machine; `localhost` would be the Consul container itself.
   - `HttpsRedirection:Enabled`: `false`.
3. Configure the gitignored `src/Nurtricenter.WorkerService/appsettings.Development.json`. Copy it
   from `appsettings.Development.template.json`, then set Neon and `RabbitMqSettings:Host` =
   `localhost`.
4. Run, listening on all interfaces so Consul's health check can reach the API:

   ```powershell
   $env:ASPNETCORE_URLS = 'http://0.0.0.0:5036'
   dotnet run --project src/Nurtricenter.Api/Nurtricenter.Api.csproj --launch-profile http
   # second terminal:
   dotnet run --project src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj
   ```

The API logs `Registered ... in Consul`, and within 30 s the gateway routes to your local
process. Calling `http://localhost:5036/api/v1/...` directly works too.

### 8.2 ms5 alone, from the Docker image (no platform)

`docker-compose.yml` in the ms5 repo root runs only the API and worker from the published image,
without Consul or the gateway. The image build needs the feed token as BuildKit secrets, which
never end up in the image:

```powershell
cd "C:\Dev\dotnet projects\NUR\FinalProject\modulo-2-actividad-3-DruGuerrero"
docker build --secret id=nuget_username,env=NUGET_USERNAME --secret id=nuget_token,env=NUGET_TOKEN -t aguerreroc/nurtricenter-ms5:0.2.0 .
docker compose up
```

Fill in the `CHANGE_ME` placeholders in `docker-compose.yml` locally first, and don't commit
them. The API is at <http://localhost:8080> (`/health`, `/api/v1/...`).

---

## 9. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `pwsh : The term 'pwsh' is not recognized` | PowerShell 7 isn't installed | use `powershell -ExecutionPolicy Bypass -File "<script>"` |
| `... cannot be loaded because running scripts is disabled` | execution policy | same: `-ExecutionPolicy Bypass` (applies to that run only) |
| `open ...\catalog.compose.yml` / `...\ms4.compose.yml` / `...\nurtricenter.compose.yml: cannot find the file` | a sibling repo is missing, outdated, or ms5's folder name doesn't match | `git pull` the repos, re-run `setup-project.ps1`, set `NURTRICENTER_REPO_DIR` (§3.3) |
| `failed to connect to the docker API at npipe:...` | Docker Desktop isn't running | start it and wait for *Engine running* |
| `NU1301` / `401` restoring `Nur.Tricenter.*` | token missing, wrong scope, or terminal opened before it was set | §3.1, then reopen the terminal |
| `NU1102: Unable to find package Nur.Tricenter.X with version (>= N)` though it's published | NuGet cached the feed's version list from before the release | `dotnet restore <solution> --no-http-cache` |
| Port already allocated (5672, 8500, 15672, 5433, 15005) | another container or process holds it, e.g. old `consul-dev` / `rabbitmq-dev` | `docker ps -a`, stop or remove the old ones |
| Browser on `/api/nurtricenter/v1/couriers` → *page isn't working* / `405` | the URL is POST-only | use the POST in §5.3 |
| Browser on `/api/catalog/...` → *page isn't working* / `401` | catalog requires a JWT | §5.5 |
| Gateway `404` right after start | the gateway refreshes routes from Consul every 30 s, or the service isn't registered/healthy | wait 30 s; check Consul (§5.2) |
| Worker log: `relation "outbox.outboxMessage" does not exist` | the database ms5 points to has no schema | check `.env` points to Neon (§3.4); apply migrations (§3.5) |
| ms5 containers healthy but every request fails with a DB error | `.env` missing, misplaced or quoted | `.env` in the ms5 **repo root**, no quotes, then recreate (§6) |
| Outbox rows marked processed but nothing reaches RabbitMQ | publish handler registered for the wrong type | see [connectivity-and-messaging.md §7](connectivity-and-messaging.md#7-troubleshooting) |
| `Cannot load library libgssapi_krb5.so.2` in the ms5 API log | the Postgres driver probes for an optional Kerberos library | harmless, ignore |
| `Could not load file or assembly 'IdentityModel'` in the gateway log | the gateway's HealthChecks UI dashboard | harmless for routing, ignore |
| A container restarts forever right after start | its `bin/Debug` output is stale or missing (bind-mounted) | rebuild that project, `docker restart <container>` |

---

## 10. Reference: ports, URLs, names

| What | Where |
|---|---|
| Gateway (the only public entry point) | <http://localhost:15005> |
| ms5 through the gateway | `http://localhost:15005/api/nurtricenter/v1/...` |
| Consul UI | <http://localhost:8500> |
| RabbitMQ UI | <http://localhost:15672> (guest / guest), AMQP on `5672` |
| Platform Postgres (not ms5's database) | `localhost:5433`, `dev-only` / `dev-only-password` |

| ms5 container | Hostname on `nur-tricenter-net` | Role |
|---|---|---|
| `nur-tricenter-nurtricenter-api` | `nurtricenter-api:8080` | HTTP API, registered in Consul as `nurtricenter` |
| `nur-tricenter-nurtricenter-workerservice` | `nurtricenter-workerservice` | outbox → RabbitMQ, no HTTP, not in Consul |

ms5 API endpoints (prefix them with `http://localhost:15005/api/nurtricenter` to call them
through the gateway):

| Method | Path |
|---|---|
| POST | `/v1/couriers` |
| GET | `/v1/couriers/{courierId}/route/today` |
| POST | `/v1/routes` |
| POST | `/v1/routes/{routeId}/start` |
| POST | `/v1/packages/deliveries` |
| POST | `/v1/routes/{routeId}/deliveries/{deliveryId}/deliver` |
| POST | `/v1/routes/{routeId}/deliveries/{deliveryId}/fail` |

Events published (one fanout exchange each): `courier-created`, `courier-status-changed`,
`route-created`, `courier-assigned-to-route`, `delivery-added-to-route`, `route-started`,
`route-completed`, `route-cancelled`, `delivery-completed`, `delivery-failed`. The message types
are in the shared kernel, `Nur.Tricenter.Integration.Messages.Logistics`.

| Config file | Tracked? | Used when |
|---|---|---|
| `.env` (repo root) | no | ms5 runs in the platform stack |
| `.env.example` | yes | template for `.env` |
| `nurtricenter.compose.yml` | yes | the platform includes it |
| `docker-compose.yml` | yes | ms5 standalone from the image (§8.2) |
| `src/*/appsettings.Development.json` | no | `dotnet run` / IDE (§8.1), `dotnet ef` |
| `NuGet.config` | yes | restore; reads `NUGET_USERNAME` / `NUGET_TOKEN` from the environment |
