# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

Build the full solution (`.slnx` at `src/Nurtricenter.Api/Nurtricenter.Api.slnx`):

```bash
dotnet build src/Nurtricenter.Api/Nurtricenter.Api.slnx
```

Run the API:

```bash
dotnet run --project src/Nurtricenter.Api/Nurtricenter.Api.csproj
```

Run the outbox worker (publishes domain events to RabbitMQ; needs `RabbitMqSettings__*` and `ConnectionStrings__DefaultConnection`):

```bash
dotnet run --project src/Nurtricenter.WorkerService/Nurtricenter.WorkerService.csproj
```

DO NOT RUN MIGRATIONS, the user will manually run the commands below
EF Core migrations (run from `src/Nurtricenter.Infrastructure`):

```bash
dotnet ef migrations add <Name> --startup-project ../Nurtricenter.Api
dotnet ef database update --startup-project ../Nurtricenter.Api
```

The outbox table (`outbox.outboxMessage`) and the inbox table (`InboxMessages`) are created by
migrations — the API and the worker require `dotnet ef database update` before running against a
real database.

## Architecture

Clean Architecture with DDD tactical patterns, .NET 10, PostgreSQL (Npgsql).

```
Nurtricenter.Api           ← ASP.NET Core Web API, Scalar (OpenAPI), Serilog
                             (NO business logic — controllers delegate to Application use cases only)
Nurtricenter.Application   ← MediatR + FluentValidation + UseCases + Handlers
                             (+ outbox handlers: SaveInOutboxWhen* / PublishIntegrationEventWhen*)
Nurtricenter.Core          ← Domain + use case interfaces + repo interfaces
Nurtricenter.Infrastructure ← EF Core DbContext, entity configurations, migrations,
                             outbox persistence, Consul self-registration (ServiceDiscovery/),
                             Postgres health check, consumers + inbox (Messaging/)
Nurtricenter.WorkerService ← Worker host: OutboxProcessor + RabbitMQ publisher + consumers
                             (ms5.package-validated) + DefaultQueueRegistrar (declares
                             nurtricenter.default, never consumes it)
contracts/Nurtricenter.Contracts ← NurtricenterExchanges registry (+ the kernel's contracts
                             via Nur.Tricenter.Integration)
```

Dependency flow: **Api → Application → Core**, **Api → Infrastructure → Application → Core**,
**WorkerService → Infrastructure** (Application flows transitively), **Application → Contracts**.

### Connectivity (ms2-style platform integration)

- **Consul self-registration**: opt-in via `ServiceRegistration:ServiceDiscoveryAddress` (blank = off).
  The same predicate gates the `postgres` health check. The shared YARP+Consul gateway routes
  `/api/{serviceName}/*` → strips prefix → `/api/*`. All endpoints must stay versioned
  (`/api/v1/...`) for the rewrite to work.
- **Transactional outbox → RabbitMQ**: domain events are staged as `OutboxMessage<DomainEvent>`
  rows in the same transaction as the aggregates (UnitOfWork drains events BEFORE SaveChanges).
  The worker dispatches them to `PublishIntegrationEventWhen*` handlers, which publish contracts
  to fanout exchanges (see `NurtricenterExchanges`). One exchange per event; `nurtricenter.default`
  is the never-consumed history sink.
- **Consumers (worker only)**: `ms5.package-validated` (MS3's `PackageValidated`, kernel namespace
  `Messages.Contracting`) → `PackageValidatedConsumer` → `AddValidatedPackageCommand` (adds the
  package to the latest route of the message's `DeliveryDate`; null coords fall back to `branchCoordinates`;
  the `PatientName` and MS4's `DeliveryDayId` are stored with the delivery, and the day id is forwarded
  in `DeliveryAddedToRoute` so MS4 can link the delivery to its day). Pipeline `TracingConsumer → IdempotentConsumer → consumer` (MS4 pattern, registered by
  `RabbitMqExtensions.AddNurtricenterConsumers`). The inbox row is staged in the scoped `AppDbContext`
  so the handler's `UnitOfWork.CommitAsync` saves it atomically with the aggregate and outbox rows.
  Queues/bindings are owned by `nur-tricenter-local` topology, never declared here.
  `POST /api/v1/packages/deliveries` stays as a manual fallback (today's route, patient name in the body).
  MS5 calls no other service at request time: there is no ClinicService anymore.
- **Domain events are explicit-ctor records with NO public parameterless ctor** (outbox Newtonsoft
  deserialization would otherwise build empty events). The serialization guard test enforces this.
- Joseco's `OutboxProcessor` dispatches each row as `OutboxMessage<{concrete event type}>`, NOT
  `OutboxMessage<DomainEvent>` — publish handlers must implement
  `INotificationHandler<OutboxMessage<XxxEvent>>` or they are silently skipped while the row is
  still marked processed (guarded by `OutboxProcessorDispatchTests`). The stored `CorrelationId`
  is dropped on re-wrap; `OutboxCorrelation.Resolve` recovers it from the `Joseco.Outbox` activity.
- The integration contracts come from the shared kernel package `Nur.Tricenter.Integration`
  1.4.0 (namespaces `Messages.Logistics` published, `Messages.Contracting` consumed), restored from the private
  Nur-tricenter GitHub Packages feed (`NuGet.config`, needs `NUGET_USERNAME`/`NUGET_TOKEN`).
  Changing a contract = a PR to `nur-tricenter-shared-kernels` + a version bump here.
  `contracts/` keeps only `NurtricenterExchanges` (the kernel forbids domain words outside messages).
- Docker builds need the feed credentials as BuildKit secrets:
  `docker build --secret id=nuget_username,env=NUGET_USERNAME --secret id=nuget_token,env=NUGET_TOKEN ...`
- `nurtricenter.compose.yml` is the platform variant included by `nur-tricenter-local`. It bind-mounts
  `bin/Debug/net10.0`, reads the Neon connection string from the gitignored `.env`, and is found
  as `../ms5-logistics-and-delivery` (the official repo, Nur-tricenter/ms5-logistics-and-delivery)
  unless `NURTRICENTER_REPO_DIR` names another folder. This checkout has a single remote, `origin`,
  which is the official repo (Nur-tricenter/ms5-logistics-and-delivery); feature branches go to
  `dev` via PR, then `dev` into `main`.
  The standalone `docker-compose.yml` runs ms5 on its own. Running instructions: `docs/running-locally.md`.
- ms5 uses its own Neon database by default; `.env.local-postgres.example` is the offline option that
  points at the platform Postgres (`ms5_logistics`). Either way the `nurtricenter-migrate` job in
  `nurtricenter.compose.yml` (`dotnet Nurtricenter.Api.dll --migrate`) applies the migrations before the
  API and worker start. Its endpoints are anonymous;
  catalog/identity/ms4 require a JWT from identity (`admin`/`admin` locally).
- On Windows here, `pwsh` is not installed: run platform scripts with
  `powershell -ExecutionPolicy Bypass -File <script>`.
- Joseco's `AddOutbox<T>()` is broken on MediatR 14 — always register outbox services manually
  (see `Nurtricenter.Infrastructure.DependencyInjection.AddOutboxServices`).
- Exchanges MUST be declared fanout + non-durable + non-auto-delete (match the Joseco 1.1.0
  publisher) or RabbitMQ answers `406 PRECONDITION_FAILED`. The library ignores
  `RabbitMqSettings:Port` — the broker must be reachable on host port 5672.
- Integration tests blank `ServiceRegistration:ServiceDiscoveryAddress` via in-memory config in
  `IntegrationTestWebAppFactory` — the gitignored appsettings.Development.json points at a real
  Consul and must never leak into the test host.

### Layer responsibilities

| Layer | Contains | Must NOT contain |
|---|---|---|
| **Api** | Controllers, middleware, DI wiring, config | Business logic, domain knowledge |
| **Application** | `Commands/{Resource}/{Action}/`, `Queries/{Resource}/{Action}/` — commands, queries, handlers, validators, DTOs | Domain logic |
| **Core** | Domain (aggregates, entities, VOs, events), repo interfaces | External dependencies, EF references, MediatR |
| **Infrastructure** | EF configs, migrations, repo implementations | Domain logic

### External DDD library

`Joseco.DDD.Core` (NuGet `1.0.2`, targets `net8.0`) lives at `C:\Dev\dotnet projects\NUR\joseco.ddd.core`. It provides:

| Type | Purpose |
|---|---|
| `Entity` / `AggregateRoot` | Base classes with `Id`, `DomainEvents`, `AddDomainEvent()` |
| `DomainEvent : INotification` | Event base record — has `Id` and `OccuredOn` |
| `IRepository<T>` where `T : AggregateRoot` | Generic repo contract (`GetByIdAsync`, `AddAsync`) |
| `DomainException(Error)` | Exception wrapping a structured `Error` |
| `Error` record | `Code`, `Description`, `Type` (Validation/NotFound/Problem/Conflict/Failure) |
| `Result` / `Result<T>` | Discriminated union (available but not yet adopted in this codebase) |

**Do not modify this library.** The `IRepository<T>` constraint on `AggregateRoot` is intentional — only aggregate roots get repositories.

## CQRS

The application layer follows CQRS with MediatR (already installed) as the message bus. Commands and queries are separate code paths:

- **Commands** — mutate domain aggregates via repositories, return `Result` or `Result<T>`. Handlers load the aggregate, call domain methods, and commit via the unit of work.
- **Queries** — return **query models** (read-only DTOs projected from the database), not domain entities. Query models live in the Application layer, not the domain. Handlers query `DbSet<T>` directly (no repository abstraction needed for reads).

### Conventions

```
Application/
├── Commands/{Resource}/{Action}/
│   ├── CreateCourierCommand.cs
│   ├── CreateCourierHandler.cs       (IRequestHandler<CreateCourierCommand, Result>)
│   └── CreateCourierValidator.cs
└── Queries/{Resource}/{Action}/
    ├── GetRouteByIdQuery.cs          (IRequest<RouteDto>)
    ├── GetRouteByIdHandler.cs        (IRequestHandler<GetRouteByIdQuery, RouteDto>)
    └── RouteDto.cs
```

- Each command has a FluentValidation validator in the same folder.
- Query models are plain classes/records suffixed `Dto` in the query folder.
- Command handlers depend on repository interfaces (from Core). Query handlers depend on `AppDbContext` directly.
- MediatR's `IRequest<,>` and `IRequestHandler<,>` are the only handler contracts — no separate interfaces in Core.

## Domain Model

Three aggregates, two with persistence:

| Type | Base class | Repository | DB access |
|---|---|---|---|
| `Route` | `AggregateRoot` | `IRouteRepository : IRepository<Route>` | `DbSet<Route>` |
| `Courier` | `Entity` | `ICourierRepository` (standalone — does not extend `IRepository<T>`) | `DbSet<Courier>` |
| `Delivery` | `Entity` (child of Route) | None | Only via `Route.Deliveries` navigation |

**Route is the sole aggregate root for the delivery cluster.** All Delivery mutations go through Route methods — there is no `IDeliveryRepository` and no `DbSet<Delivery>`:

```
Route (AggregateRoot)
├── _deliveries → Delivery (Entity, internal ctor)
│   ├── Package (VO), Address (VO), Confirmation? (VO)
│   └── Lifecycle: Pending → InProgress → Delivered/Failed
│       (controlled internally — only Route calls StartDelivery/Register*)
└── Lifecycle: Pending → InProgress → Completed/Cancelled
    (StartRoute cascades StartDelivery to children)
    (CancelRoute cascades RegisterFailedDelivery to non-terminal children)
    (CompleteRoute validates all children are terminal)
```

**Courier** is a standalone entity referenced by ID from Route — no child collection, no invariants over a cluster.

## Error Handling

All domain validation and state guards throw `DomainException`, never raw exceptions:

```csharp
// State guard → ErrorType.Problem (maps to HTTP 422)
throw new DomainException(Error.Problem("Route.CannotStart", "...is {status}."));

// Validation → ErrorType.Validation (maps to HTTP 400)
throw new DomainException(new Error("Delivery.NullPackage", "...", ErrorType.Validation));

// Not found → ErrorType.NotFound (maps to HTTP 404)
throw new DomainException(Error.NotFound("Route.DeliveryNotFound", "..."));
```

`DomainExceptionHandler` (in the API project, `IExceptionHandler`) maps every `DomainException` to RFC 7807 `ProblemDetails` with the `Error.Code` as title. Non-domain exceptions fall through to the default ASP.NET Core handler.

**Ardalis.Result is NOT in global usings** — it would collide with `Joseco.DDD.Core.Results.Result`. Import it per-file if needed.

## Conventions

- **Namespace style:** file-scoped (`namespace X;` then `using` below), matching folder structure
- **Value Objects:** `sealed record` with private parameterless ctor for EF serialization (`#pragma warning disable CS8618`)
- **Domain Events:** `sealed record` inheriting `DomainEvent`, one per state transition, named `{Entity}{Action}Event`
- **EF Configurations:** `IEntityTypeConfiguration<T>` per entity, `builder.Ignore("DomainEvents")` on all, enum properties use `.HasConversion<string>()`, owned VOs use `.OwnsOne()` with column name prefixes
- **Enums:** separate `Enums/` folder per aggregate, string-stored in DB
- **Entity IDs:** caller-provided `Guid` in constructors (not generated by the entity)
