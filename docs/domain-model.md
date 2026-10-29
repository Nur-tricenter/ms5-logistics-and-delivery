# Domain Model

```mermaid
classDiagram
    %% ── Aggregate Root ──────────────────────────────────────────
    class Route {
        &lt;&lt;AggregateRoot&gt;&gt;
        +Guid Id
        +Guid CourierId
        +DateOnly ScheduledDate
        +DateTime CreatedAt
        +RouteStatus Status
        +IReadOnlyList~Delivery~ Deliveries
        +AssignCourier(Guid courierId)
        +AddDelivery(ValidatedPackage package, DeliveryAddress address)
        +StartRoute(Coordinate startingPoint)
        +CompleteRoute()
        +CancelRoute()
        +CompleteDelivery(Guid deliveryId, DeliveryConfirmation confirmation)
        +FailDelivery(Guid deliveryId, string reason)
        -FindDelivery(Guid deliveryId) Delivery
        -CalculateDeliverySequence(Coordinate startingPoint)
    }

    %% ── Entities ────────────────────────────────────────────────
    class Delivery {
        &lt;&lt;Entity&gt;&gt;
        +Guid Id
        +Guid RouteId
        +ValidatedPackage Package
        +DeliveryAddress Address
        +DeliveryStatus Status
        +int? SequenceOrder
        +DeliveryConfirmation? Confirmation
        +string? FailureReason
        +DateTime CreatedAt
        +bool IsTerminal
        +StartDelivery()$
        +RegisterSuccessfulDelivery(DeliveryConfirmation confirmation)$
        +RegisterFailedDelivery(string reason)$
    }

    class Courier {
        &lt;&lt;Entity&gt;&gt;
        +Guid Id
        +string FullName
        +CourierStatus Status
        +SetStatus(CourierStatus status)
    }

    %% ── Value Objects ───────────────────────────────────────────
    class ValidatedPackage {
        &lt;&lt;ValueObject&gt;&gt;
        +string PackageId
        +string PatientId
        +string PatientName
        +string LabelData
    }

    class DeliveryAddress {
        &lt;&lt;ValueObject&gt;&gt;
        +string Description
        +Coordinate PlanarCoordinate
    }

    class Coordinate {
        &lt;&lt;ValueObject&gt;&gt;
        +double Latitude
        +double Longitude
        +DistanceTo(Coordinate other) double
    }

    class DeliveryConfirmation {
        &lt;&lt;ValueObject&gt;&gt;
        +DateTime DeliveredAt
        +string EvidencePhotoUrl
        +string DigitalSignature
    }

    %% ── Enums ───────────────────────────────────────────────────
    class RouteStatus {
        &lt;&lt;Enumeration&gt;&gt;
        Pending
        InProgress
        Completed
        Cancelled
    }

    class DeliveryStatus {
        &lt;&lt;Enumeration&gt;&gt;
        Pending
        InProgress
        Delivered
        Failed
    }

    class CourierStatus {
        &lt;&lt;Enumeration&gt;&gt;
        Available
        OnRoute
        OnBreak
    }

    %% ── Relationships ───────────────────────────────────────────
    Route "1" *-- "0..*" Delivery : owns
    Route "1" -- "1" Courier : references by ID
    Delivery *-- "1" ValidatedPackage
    Delivery *-- "1" DeliveryAddress
    Delivery *-- "0..1" DeliveryConfirmation
    DeliveryAddress *-- "1" Coordinate
    Delivery ..> DeliveryStatus
    Route ..> RouteStatus
    Courier ..> CourierStatus

    %% ── Legend ──────────────────────────────────────────────────
    note for Route "$ = internal (only Route calls these)"
```

## Domain Events

| Event | Trigger | Payload |
|---|---|---|
| `CourierCreatedEvent` | Courier constructed | `Id`, `FullName`, `Status` |
| `CourierStatusChangedEvent` | `Courier.SetStatus()` | `CourierId`, `OldStatus`, `NewStatus` |
| `RouteCreatedEvent` | Route constructed | `RouteId`, `CourierId`, `ScheduledDate` |
| `CourierAssignedToRouteEvent` | `Route.AssignCourier()` | `RouteId`, `CourierId` |
| `DeliveryAddedToRouteEvent` | `Route.AddDelivery()` | `RouteId`, `DeliveryId`, `PackageId`, `PatientId` |
| `RouteStartedEvent` | `Route.StartRoute()` | `RouteId` |
| `RouteCompletedEvent` | `Route.CompleteRoute()` | `RouteId` |
| `RouteCancelledEvent` | `Route.CancelRoute()` | `RouteId` |
| `DeliveryCompletedEvent` | `Delivery.RegisterSuccessfulDelivery()` | `DeliveryId`, `RouteId`, `DeliveredAt` |
| `DeliveryFailedEvent` | `Delivery.RegisterFailedDelivery()` | `DeliveryId`, `RouteId`, `Reason` |

## Repository Interfaces

| Interface | Base | Key methods beyond base |
|---|---|---|
| `IRouteRepository` | `IRepository<Route>` | `GetAllAsync`, `GetLatestRouteForDateAsync`, `GetByCourierAndDateAsync`, `UpdateAsync`, `DeleteAsync` |
| `ICourierRepository` | _(standalone)_ | `GetByIdAsync`, `GetAllAsync`, `GetByStatusAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync` |

> **Note:** `Delivery` has no repository and no `DbSet`. All persistence goes through `Route` (the aggregate root).
