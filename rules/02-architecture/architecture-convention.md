# Architecture Convention

> **Rule Hierarchy Level:** 02
> **Applies To:** All
> **Last Updated:** 2025-07-14

## Purpose

This document defines the architectural conventions that govern how the system is structured,
how layers interact, and how dependencies flow across the codebase. It ensures that every module,
service, and component adheres to Clean Architecture principles, maintaining a clear separation of
concerns and enabling the system to evolve without architectural decay.

All rules in this document build upon the SOLID foundation established in **Engineering Principles (01)**.

## Scope

This convention covers:
- Layered architecture structure and layer responsibilities
- Clean Architecture dependency rules and boundaries
- Module design, cohesion, and coupling constraints
- Cross-cutting concern management strategies
- Architectural anti-patterns and how to avoid them

This convention does **NOT** cover:
- Naming conventions or code style — see **Coding Convention**
- API endpoint design or HTTP semantics — see **API Convention**
- Database schema design or migration strategy — see **Database Convention**
- Security architecture (authentication, authorization) — see **Security Convention**
- Frontend component architecture — see **Frontend Convention**

## Principles

### P1 — Dependency Inversion Over Dependency Convenience
Dependencies MUST always point inward — from volatile outer layers toward stable inner layers.
No shortcut is worth violating this rule because reversing a dependency direction after the system
grows is exponentially more expensive than enforcing it from day one.

### P2 — Domain Purity Above All
The Domain layer is the most stable and valuable layer. It represents the business itself and
MUST remain free of frameworks, databases, and UI concerns. A pure domain model can survive
any technology migration.

### P3 — Boundaries Are Contracts, Not Suggestions
Layer boundaries are enforced through interfaces and project references — not through developer
discipline alone. If the compiler cannot catch a boundary violation, the boundary is not real.

### P4 — Cohesion Within, Decoupling Between
Each module and layer MUST have high internal cohesion (everything inside serves one purpose)
and low external coupling (minimal knowledge of other modules). This enables independent
development, testing, and deployment.

### P5 — Cross-Cutting Concerns Are Infrastructure, Not Business Logic
Logging, caching, validation, and similar concerns MUST be handled through dedicated mechanisms
(middleware, decorators, interceptors) — never scattered through business logic or domain code.

---

## Mandatory Rules

### Layered Architecture

**ARCH-001: Layer Structure MUST Follow Four-Layer Hierarchy**
The system MUST be organized into exactly four layers with the following dependency direction:

```
Presentation → Application → Domain ← Infrastructure
```

Infrastructure depends on Domain (and optionally Application) to implement interfaces.
Presentation depends on Application to invoke use cases. Domain depends on nothing.

- **Rationale:** A consistent layer structure gives every developer a shared mental model of where
  code belongs, eliminating architectural debates during code reviews.
- **Consequence:** Code placed in the wrong layer MUST be moved during code review. PRs violating
  layer structure MUST NOT be merged.

---

**ARCH-002: Domain Layer MUST Have ZERO External Dependencies**
The Domain layer MUST NOT reference any external NuGet package, framework library, or infrastructure
concern. The only allowed dependencies are the language runtime and standard library.

- **Rationale:** Domain purity ensures the business rules survive any technology migration. If the
  ORM, web framework, or database changes, the domain remains untouched.
- **Consequence:** Any `using` statement referencing an external package in the Domain project MUST
  be removed and refactored to use a domain-defined abstraction.

---

**ARCH-003: Presentation Layer MUST NOT Contain Business Logic**
Controllers, pages, views, and API endpoints MUST be thin. They MUST only: receive input, delegate
to Application layer services, and return output. No conditional business rules, calculations, or
domain operations are allowed.

- **Rationale:** Business logic in the presentation layer cannot be reused across different UIs
  (Web, API, Mobile) and cannot be unit tested without spinning up the entire HTTP pipeline.
- **Consequence:** Any business logic found in a controller or page MUST be extracted to the
  Application layer during code review.

---

**ARCH-004: Infrastructure Layer MUST Be the Only Layer Aware of External Technologies**
Only the Infrastructure layer MAY reference specific databases (SQL Server, Redis), ORMs
(Entity Framework Core), external APIs, file systems, message brokers, or cloud SDKs.

- **Rationale:** Isolating technology-specific code to Infrastructure means swapping a database or
  switching a cloud provider only affects one layer.
- **Consequence:** Direct references to `DbContext`, `HttpClient`, or any SDK in Domain or
  Application layers MUST be replaced with abstractions.

---

**ARCH-005: All Cross-Layer Communication MUST Use Dependency Injection**
Layers MUST NOT instantiate dependencies from other layers directly. All cross-layer dependencies
MUST be resolved through Dependency Injection (constructor injection preferred).

- **Rationale:** DI enforces the Dependency Inversion Principle, enables testability through
  mocking, and makes dependency graphs explicit and auditable.
- **Consequence:** Any use of `new ConcreteService()` for cross-layer dependencies MUST be
  refactored to constructor injection with an interface.

---

### Clean Architecture

**ARCH-006: Application Layer MUST Define Use Cases as the Primary Entry Point**
Each business operation MUST be represented as a discrete use case (service method or command/query
handler) in the Application layer. The Presentation layer invokes use cases — it does not
orchestrate domain objects directly.

- **Rationale:** Use cases provide a single, testable, reusable unit of business behavior that is
  independent of the delivery mechanism (API, UI, CLI, message handler).
- **Consequence:** Presentation code that directly manipulates domain entities MUST be refactored
  into an Application layer use case.

---

**ARCH-007: Interfaces MUST Be Defined in Domain or Application, Implemented in Infrastructure**
Repository interfaces, external service abstractions, and infrastructure contracts MUST be defined
in the Domain or Application layer. Their concrete implementations MUST reside in Infrastructure.

- **Rationale:** This is the architectural enforcement of the Dependency Inversion Principle. Inner
  layers define what they need; outer layers provide it.
- **Consequence:** Interfaces defined in Infrastructure or Presentation that are consumed by inner
  layers MUST be relocated.

---

**ARCH-008: DTOs MUST Be Used for All Data Crossing Layer Boundaries**
Domain entities MUST NOT be exposed directly to the Presentation layer or returned from API
endpoints. Data Transfer Objects (DTOs) MUST be used for all data flowing between layers.

- **Rationale:** Exposing entities creates tight coupling between the database schema and the API
  contract, making either impossible to change independently. It also risks leaking sensitive fields.
- **Consequence:** Any endpoint or page returning a raw entity MUST be refactored to use a DTO with
  explicit mapping.

---

### Module Boundaries

**ARCH-009: Modules MUST NOT Have Circular Dependencies**
No two modules (projects, assemblies, namespaces, or feature folders) MAY depend on each other.
If Module A references Module B, Module B MUST NOT reference Module A — directly or transitively.

- **Rationale:** Circular dependencies make it impossible to build, test, or deploy modules
  independently, and they create ripple effects where a change in one module forces recompilation
  of the other.
- **Consequence:** Circular dependencies MUST be broken by extracting shared abstractions into a
  common module or by introducing an interface at the boundary.

---

**ARCH-010: Communication Between Modules MUST Use Well-Defined Interfaces**
Modules MUST NOT access each other's internal types, private services, or implementation details.
All inter-module communication MUST go through public interfaces, events, or shared contracts.

- **Rationale:** Accessing internal details creates hidden coupling that breaks when the target
  module refactors its internals.
- **Consequence:** Direct references to another module's internal classes MUST be replaced with
  interface-based communication or shared DTOs.

---

### Dependency Rules

**ARCH-011: All User-Facing Strings MUST Be Externalized**
Error messages, validation messages, UI labels, and all text displayed to users MUST be stored in
resource files, constants classes, or a localization system. No hardcoded strings in business logic
or presentation code.

- **Rationale:** Hardcoded strings prevent localization, create inconsistencies when the same message
  is duplicated, and make global text changes tedious and error-prone.
- **Consequence:** Any hardcoded user-facing string found during code review MUST be extracted to
  the appropriate resource file before the PR is approved.

---

**ARCH-012: Existing Code MUST Be Reused Before Creating New Implementations**
Before writing a new service, component, utility, or helper, developers MUST search the codebase
for existing implementations that serve the same or overlapping purpose. Duplication is only
permitted when the existing code genuinely cannot be adapted.

- **Rationale:** Duplication leads to inconsistent behavior, multiplied bug fixes, and inflated
  maintenance costs.
- **Consequence:** Duplicate implementations discovered during review MUST be consolidated into
  the existing implementation with the new consumer refactored to use it.

---

## Recommended Practices

**ARCH-050: Vertical Slicing SHOULD Be Used for Feature Organization**
Within each layer, code SHOULD be organized by feature (vertical slice) rather than by technical
concern (horizontal slice). For example, group all Order-related code together rather than having
separate folders for all controllers, all services, all repositories.

- **Rationale:** Vertical slicing improves discoverability, reduces merge conflicts across teams,
  and makes it easier to understand the full scope of a feature.
- **Consequence:** While not a blocking issue, horizontal-only organization SHOULD be refactored
  toward vertical slicing when the feature set grows.

---

**ARCH-051: Mediator Pattern SHOULD Be Used for Complex Use Case Orchestration**
When a use case involves coordinating multiple services or domain operations, the Mediator pattern
(e.g., MediatR or a custom implementation) SHOULD be used to decouple the sender from the handler.

- **Rationale:** Direct service-to-service calls create deep dependency chains. A mediator keeps
  each handler focused on a single responsibility.
- **Consequence:** Overly complex service methods with many injected dependencies SHOULD be
  evaluated for mediator-based decomposition.

---

**ARCH-052: Domain Events SHOULD Be Used for Side Effects**
When a domain action triggers side effects (e.g., sending a notification after an order is placed),
domain events SHOULD be raised from the domain entity and handled by separate event handlers in the
Application or Infrastructure layer.

- **Rationale:** Domain events keep the core action pure and prevent the domain from knowing about
  infrastructure-level side effects.
- **Consequence:** Side effects embedded directly in domain methods SHOULD be extracted to event
  handlers when the method becomes difficult to test.

---

**ARCH-053: Cross-Cutting Concerns SHOULD Be Handled via Middleware or Decorators**
Logging, caching, performance monitoring, and retry policies SHOULD be implemented as middleware,
decorators, or interceptors — not as inline code within business methods.

- **Rationale:** Scattering cross-cutting logic through business code violates Single Responsibility
  and makes it impossible to change the strategy (e.g., switching loggers) without modifying every
  service.
- **Consequence:** Services with inline logging/caching logic SHOULD be refactored to use the
  pipeline or decorator approach.

---

**ARCH-054: Each Layer SHOULD Have Its Own Model Types**
The Presentation layer SHOULD use ViewModels/API models, the Application layer SHOULD use DTOs and
Commands/Queries, and the Domain layer SHOULD use Entities and Value Objects. Sharing the same model
across layers SHOULD be avoided.

- **Rationale:** Each layer has different concerns — validation attributes, serialization settings,
  and domain invariants are fundamentally different and should not be mixed in one class.
- **Consequence:** Using a single shared model across all layers SHOULD be refactored into
  layer-specific models with explicit mapping.

---

## Anti-patterns

### AP-01: God Class
**Description:** A single class that handles too many responsibilities — orchestrating business
logic, accessing the database, formatting output, and managing state all in one place.

**Why it's harmful:** Violates Single Responsibility (SRP) and Open/Closed (OCP) principles. Any
change to any concern risks breaking unrelated functionality. Testing is nearly impossible without
extensive mocking.

**What to do instead:** Decompose the class by responsibility. Extract domain logic into entities
or domain services, data access into repositories, and orchestration into application use cases.

---

### AP-02: Circular Dependencies
**Description:** Module A depends on Module B, and Module B depends back on Module A — either
directly or through a chain of intermediate modules.

**Why it's harmful:** Prevents independent compilation, testing, and deployment. Changes ripple
unpredictably across both modules. Build systems may fail or produce incorrect results.

**What to do instead:** Identify the shared abstractions and extract them into a separate shared
module. Use interfaces and dependency injection to invert the problematic dependency direction.

---

### AP-03: Business Logic in Controllers / Presentation
**Description:** Controllers or Blazor page code-behind files contain conditional logic, domain
calculations, or business rule enforcement instead of delegating to the Application layer.

**Why it's harmful:** Business rules locked inside a controller cannot be reused by other delivery
mechanisms (CLI tools, background jobs, other UIs). Unit testing requires bootstrapping the entire
HTTP pipeline.

**What to do instead:** Keep controllers thin — they accept input, call a use case, and return
output. All business decisions live in the Application or Domain layer.

---

### AP-04: Direct Database Access from Presentation
**Description:** A controller or page directly instantiates a `DbContext` or executes SQL queries
instead of going through a repository or application service.

**Why it's harmful:** Bypasses all business rules, validation, and auditing. Couples the UI
directly to the database schema, making schema changes break the presentation layer.

**What to do instead:** Access data exclusively through repository interfaces resolved via DI,
invoked through application layer use cases.

---

### AP-05: Anemic Domain Model (When Domain Logic Exists)
**Description:** Domain entities are mere data bags (only properties, no behavior) while all
business rules live in application services operating on those entities externally.

**Why it's harmful:** Violates encapsulation. Business rules become scattered across multiple
services instead of living close to the data they protect. Different services may enforce rules
inconsistently.

**What to do instead:** Push business rules that guard an entity's invariants into the entity
itself. Use value objects for concepts with validation rules. Keep application services for
orchestration that spans multiple entities.

---

### AP-06: Leaking Infrastructure Abstractions Upward
**Description:** Domain or Application layer code references infrastructure-specific types such as
`SqlConnection`, `HttpClient`, `IFormFile`, or `JsonSerializerOptions`.

**Why it's harmful:** Ties business logic to specific technologies, preventing substitution and
making unit tests depend on infrastructure availability.

**What to do instead:** Define an abstraction in Domain or Application (e.g., `IFileStorage`,
`IPaymentGateway`) and implement it in Infrastructure.

---

## Examples

### ✅ Correct: Clean Layer Separation

```csharp
// Domain Layer — Pure entity with business logic, no external dependencies
public class Order
{
    public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = new();

    public void AddItem(MenuItem menuItem, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(DomainErrors.Order.CannotModifyNonDraftOrder);

        var existing = _items.FirstOrDefault(i => i.MenuItemId == menuItem.Id);
        if (existing is not null)
            existing.IncreaseQuantity(quantity);
        else
            _items.Add(new OrderItem(menuItem.Id, menuItem.Price, quantity));
    }
}

// Application Layer — Use case with injected abstractions
public class PlaceOrderHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceOrderHandler(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, ct);
        order.Place();
        await _unitOfWork.SaveChangesAsync(ct);
        return order.ToDto();
    }
}

// Presentation Layer — Thin controller, no business logic
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{orderId}/place")]
    public async Task<IActionResult> PlaceOrder(Guid orderId)
    {
        var result = await _mediator.Send(new PlaceOrderCommand(orderId));
        return Ok(result);
    }
}
```

### ❌ Incorrect: Business Logic in Controller with Direct DB Access

```csharp
// VIOLATION: Controller contains business logic AND directly accesses DbContext
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context; // ARCH-004 violation

    [HttpPost("{orderId}/place")]
    public async Task<IActionResult> PlaceOrder(Guid orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId); // ARCH-003 violation

        if (order.Status != "Draft") // ARCH-011 violation: hardcoded string
            return BadRequest("Cannot place a non-draft order"); // Hardcoded message

        order.Status = "Placed"; // Business logic in controller
        order.PlacedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        return Ok(order); // ARCH-008 violation: returning raw entity
    }
}
```

### ✅ Correct: Interface Defined in Domain, Implemented in Infrastructure

```csharp
// Domain Layer — Interface definition
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
}

// Infrastructure Layer — Concrete implementation with EF Core
public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context) => _context = context;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct)
        => await _context.Orders.AddAsync(order, ct);
}
```

### ✅ Correct: Cross-Cutting Concerns via Pipeline Behavior

```csharp
// Application Layer — Logging as a decorator, not inline code
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        _logger.LogInformation(LogMessages.HandlingRequest, typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation(LogMessages.CompletedRequest, typeof(TRequest).Name);
        return response;
    }
}
```

### ❌ Incorrect: Scattered Cross-Cutting Concerns

```csharp
// VIOLATION: Logging, caching, and validation manually embedded in every handler
public class PlaceOrderHandler
{
    public async Task<OrderDto> Handle(PlaceOrderCommand command)
    {
        _logger.LogInformation("Handling PlaceOrder");     // Repeated in every handler
        _cache.Remove($"order-{command.OrderId}");         // Cache logic in business code
        if (command.OrderId == Guid.Empty)                  // Validation in handler
            throw new ArgumentException("Invalid order");   // Hardcoded string

        // ... actual business logic buried under cross-cutting noise
        _logger.LogInformation("Completed PlaceOrder");    // Repeated in every handler
    }
}
```

---

## Checklist

### Layer Structure
- □ The solution has exactly four layers: Presentation, Application, Domain, Infrastructure
- □ Domain project has zero NuGet package references (except the runtime)
- □ Infrastructure is the only project referencing ORM, database, and external SDK packages
- □ Presentation project references Application — never Domain or Infrastructure directly

### Dependency Direction
- □ No project reference flows outward (inner layer referencing outer layer)
- □ All cross-layer dependencies are resolved through constructor injection
- □ Interfaces consumed by inner layers are defined in Domain or Application
- □ Concrete implementations of those interfaces reside in Infrastructure

### Clean Architecture
- □ Every public API endpoint or page delegates to an Application layer use case
- □ Controllers and pages contain no business conditional logic
- □ Domain entities are never returned directly from endpoints — DTOs are used
- □ Domain entities encapsulate their own invariants (non-anemic where applicable)

### Module Boundaries
- □ No circular references exist between projects or namespaces
- □ Inter-module communication uses public interfaces or shared contracts only
- □ Internal classes and methods are scoped appropriately (`internal` vs `public`)

### Cross-Cutting Concerns
- □ Logging is handled via middleware, pipeline behaviors, or decorators
- □ Caching strategies are implemented through decorators, not inline in use cases
- □ Validation is handled via a dedicated validation pipeline (e.g., FluentValidation)
- □ No cross-cutting logic is duplicated across multiple handlers or services

### Code Reuse & Strings
- □ Before creating a new service or component, existing codebase was searched for reuse
- □ All user-facing strings are externalized to resource files or constants classes
- □ No magic strings exist in business logic or presentation code
