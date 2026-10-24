# Nurtricenter Logistics & Delivery - Testing Strategy

This document outlines the testing strategy, architecture, and key decisions for the Nurtricenter Logistics & Delivery test suite. The suite ensures system reliability through a combination of fast unit tests and end-to-end integration tests.

## 1. Overview

The test suite consists of **273 tests** (268 Unit Tests, 5 Integration Tests) and is built using:
- **xUnit**: The core testing framework.
- **FluentAssertions**: For highly readable, fluent-style assertions.
- **Moq**: For mocking dependencies like Repositories and external HTTP services.
- **Microsoft.AspNetCore.Mvc.Testing**: For spinning up an in-memory test API server (`WebApplicationFactory`).
- **Entity Framework Core In-Memory**: Used to simulate the database in integration tests.

Run everything with `dotnet test src/Nurtricenter.Api/Nurtricenter.Api.slnx`. Restoring needs the
private Nur-tricenter feed (`NUGET_USERNAME` / `NUGET_TOKEN`; see [running-locally.md §3.1](running-locally.md#31-store-the-github-token-for-the-private-nuget-feed)).
The tests never touch Consul, RabbitMQ or the real database. The connectivity guards (outbox,
Consul registration, RabbitMQ wiring, the real outbox processor dispatch) are listed in
[connectivity-and-messaging.md §5.1](connectivity-and-messaging.md#51-automated-tests).

---

## 2. Unit Testing Strategy

Unit tests focus on isolating components and ensuring business logic behaves correctly in a controlled environment. 

### Core Domain Tests (High Priority)
The Domain layer has no external dependencies, making it highly testable.
- **Entities & Behaviors**: We exhaustively test entities like `Route` and `Courier`. We verify that state transitions (e.g., `StartRoute`, `CompleteRoute`, `CancelRoute`) respect domain invariants and throw specific `DomainException`s when rules are violated (e.g., attempting to complete a route that has pending deliveries).
- **Value Objects**: Types like `ValidatedPackage`, `DeliveryAddress`, and `Coordinate` are tested to ensure they cannot be instantiated with invalid states (e.g., empty IDs or invalid GPS coordinates).
- **Domain Events**: We verify that the correct domain events (like `RouteStartedEvent` or `CourierAssignedToRouteEvent`) are appended to the entity upon specific actions.

### Application Layer (Use Cases)
- **Handlers**: Command and Query handlers are tested by mocking the underlying `IRouteRepository`, `ICourierRepository`, and `IUnitOfWork`. We ensure that handlers correctly fetch data, invoke domain methods, map results to DTOs, and commit the Unit of Work.
- **Event Handlers**: We test domain event subscribers (like `CompleteRouteWhenAllDeliveriesCompletedHandler`) to ensure side-effects are properly executed.
- **Validation**: We test `FluentValidation` rules to guarantee the API fails fast before reaching the domain layer if the input is malformed.

### Key Decision: The Test Data Builder Pattern
We use a centralized `RouteTestBuilder` to generate domain entities in specific states (e.g., `CreatePendingRoute`, `CreateInProgressRoute`, `AddDeliveries`). This pattern drastically reduces Arrange-phase boilerplate and keeps tests focused on the Act and Assert phases.

---

## 3. Integration Testing Strategy

Integration tests evaluate "Vertical Slices" of the application—from the HTTP request down through the API endpoints, Application layer, Domain layer, and Database—validating the actual wiring of the application.

### WebApplicationFactory & In-Memory Database
We use `WebApplicationFactory` to host the API in-memory. 
- **Database Pivot**: We explicitly strip out PostgreSQL (`Npgsql`) configurations from the Dependency Injection (DI) container and replace them with `UseInMemoryDatabase`. This ensures the test suite runs blazingly fast and works seamlessly in environments without Docker daemons (bypassing the need for `Testcontainers`).
- **Scoping & Isolation**: To ensure complete test isolation, the Factory generates a unique Database Name via `Guid.NewGuid()` per test suite run. Additionally, our `BaseIntegrationTest` implements a `ClearDatabaseAsync` method that resets the `Routes` and `Couriers` tables before every single test run.

### Mocking External Boundaries
The system communicates with an external Clinic API to retrieve patient names (`IClinicService`).
- **Key Decision**: In integration tests, we do not want to make real HTTP calls to external boundaries. We inject a Singleton `Mock<IClinicService>` into the test DI container. Tests then configure this mock to return controlled patient data, allowing us to seamlessly test endpoints like `GET /api/v1/couriers/{id}/route/today` without flakiness or external dependencies.

---

## 4. Running the Tests

To execute the test suite, use the .NET CLI from the repository root:

**Run all tests:**
```bash
dotnet test
```

**Run only Unit Tests:**
```bash
dotnet test --filter "FullyQualifiedName!~Integration"
```

**Run only Integration Tests:**
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

