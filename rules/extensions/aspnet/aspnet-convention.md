# ASP.NET Core & C# 13 Conventions

> **Rule Hierarchy Level:** Extension (Framework-Specific)
> **Applies To:** Backend (ASP.NET Core / C# 13 / .NET 9)
> **Last Updated:** 2026-07-14

## Purpose
This document defines the framework-specific engineering conventions, class structures, C# 13 language features, and runtime configurations for the ASP.NET Core backend. It ensures Clean Architecture compliance, strict validation rules, clean error handling, and optimal dependency management.

## Scope
This convention covers C# 13 coding styles, Clean Architecture layer responsibilities, Web API Controllers, Application Services, dependency injection lifetimes, validation configuration, and ASP.NET Core middlewares. It does NOT cover database query optimization (see [SQL Server & EF Core Extension](file:///d:/My%20Project/rules/extensions/sqlserver/sqlserver-convention.md)).

## Principles
1. **Clean Layer Separation:** Layers MUST only communicate via defined interfaces and DTOs.
2. **C# 13 Modernity:** Utilize modern language features (Primary Constructors, Collection Expressions, File-scoped namespaces) to keep files concise and readable.
3. **Purity of Use Cases:** Keep business services independent of HTTP or database concerns.
4. **Structured Error Handling:** Leverage Global Exception Middlewares to convert exceptions into standardized JSON responses.

---

## Mandatory Rules

### ASPNET-001: Clean Architecture Project Boundaries
The solution structure MUST strictly segregate project references according to these architectural rules:
- **`[ProjectName].Domain`:** Holds domain entities, enums, value objects, and repository interfaces. It MUST have zero external dependencies or references to other projects.
- **`[ProjectName].Application`:** Holds business services, DTO definitions, FluentValidation rules, and mapping profiles. It depends only on Domain. It MUST NOT reference Entity Framework Core or SQL Server namespaces.
- **`[ProjectName].Infrastructure`:** Holds the EF Core DbContext, configuration mappings, migrations, repository implementations, cache handlers, and external API integrations. It depends on Application.
- **`[ProjectName].WebApi`:** Entry point of the application containing Controllers, middleware, `Program.cs`, and `appsettings.json`. It registers all dependency injection profiles.
- **Rationale:** Ensures business rules are highly portable, testable, and insulated from technical upgrades or infrastructure changes.
- **Consequence:** Leaking database configurations or EF Core tracking into the Domain layer breaks decoupling, prevents unit testing domain logic without database drivers, and locks the application to a specific database technology.

### ASPNET-002: Modern C# 13 Features
The backend codebase MUST prioritize C# 13 and .NET 9 syntax:
- Use **Primary Constructors** for constructor dependency injection in services and controllers.
- Use **Collection Expressions** (`[]`) for array, list, and collection initializations.
- Use **File-scoped namespaces** to reduce nested indentation.
- Enable **Nullable Reference Types** (`<Nullable>enable</Nullable>`) across all projects.
- **Rationale:** Keeps codebase clean, modern, and decreases syntactic boilerplate.
- **Consequence:** Legacy C# boilerplate patterns increase file sizes and hide logic beneath repetitive constructor assignments.

### ASPNET-003: Thin Controllers
Controllers MUST contain zero business validation, decision making, or database querying logic. A controller's single responsibility is to receive requests, call validation, trigger an Application Service, and return an HTTP response.
- **Rationale:** Separation of concerns. API endpoints are simply delivery channels; business logic must remain independent of HTTP frameworks.
- **Consequence:** Writing business calculations inside Controller methods prevents that logic from being reused by other entry points (like SignalR Hubs, CLI tools, or background jobs) and complicates unit testing.

### ASPNET-004: Propagation of CancellationTokens in API Actions
Every async Controller action method MUST accept a `CancellationToken` as a parameter and pass it down to every Application Service call.
- **Rationale:** Stops useless server operations when users abort queries.
- **Consequence:** A user repeatedly clicking a search button will spawn multiple database queries that run to completion on the server, wasting resources.

### ASPNET-005: Interface Segregation for Services (Read/Write Split)
Application services MUST NOT be monolithic interfaces. Services MUST be segregated based on focused roles. Specifically, read queries and write commands SHOULD be split into separate interfaces (e.g., `IOrderReadService` and `IOrderWriteService`).
- **Rationale:** Prevents service classes from becoming "God classes" containing dozens of methods, facilitating easier testing and modification.
- **Consequence:** Modifying a single order calculation forces changes to a giant monolithic `IOrderService` interface, triggering rebuilds and re-mocking across all tests.

### ASPNET-006: Global Exception Middleware (Unified Error Format)
Developers MUST NOT write try-catch blocks simply to return HTTP error codes inside controllers. A global exception handling middleware MUST capture all unhandled exceptions, write a structured log, and format a standardized JSON response conforming to the project standard:
```json
{
  "success": false,
  "message": "Localized error message",
  "errors": ["MACHINE_READABLE_ERROR_CODE"],
  "traceId": "W3C-Trace-Id"
}
```
- **Rationale:** Ensures consistent error structures for frontend clients and guarantees security by hiding raw database exception details from public users.
- **Consequence:** Inconsistent error responses break frontend error parsing and leak stack traces containing DB table names or file paths, presenting a security risk.

### ASPNET-007: No Hardcoded UI Text or Messages
All error messages, success responses, and UI notifications returned by backend services MUST be loaded from resource translation files (`.resx`) located in the Application layer.
- **Rationale:** Facilitates localized multi-language rendering and centralizes copyright edits.
- **Consequence:** Hardcoding strings makes localized translation impossible without recompiling code.

### ASPNET-008: No Console.WriteLine (Mandatory ILogger)
Using `Console.WriteLine` or `Console.Write` is strictly prohibited. All log messages MUST be generated using `ILogger<T>` structured logging APIs, incorporating correlation IDs and metadata parameters.
- **Rationale:** Enables searchability and indexing of logs in production monitors (Seq/Elasticsearch).
- **Consequence:** Raw console prints are untraceable, slow down performance, and do not capture structured metadata.

---

## Recommended Practices

### ASPNET-050: FluentValidation Pipeline Integration
Input validation SHOULD be implemented via FluentValidation, creating isolated validator classes extending `AbstractValidator<T>` located in the Application layer, and integrated directly into the ASP.NET Core request pipeline.
- **Rationale:** Decouples validation criteria from service logic, ensuring only clean requests enter business execution.

### ASPNET-051: Strongly-Typed Options Pattern
Configuration bindings SHOULD utilize the Options pattern (`IOptionsSnapshot<T>` or `IOptions<T>`) rather than reading strings directly from `IConfiguration["Section:Key"]`.
- **Rationale:** Enforces type safety on launch configurations and simplifies mocking configuration settings in tests.

---

## Anti-patterns

### Injecting DbContext into Controllers
Directly passing `ApplicationDbContext` to a controller constructor to execute LINQ queries.
- **Why it's harmful:** Breaks Clean Architecture, couples presentation directly to database schema, and prevents mock testing controllers.
- **What to do instead:** Inject an Application Service interface that returns DTOs.

### Swallowing Exceptions
Catching exceptions and returning null or empty objects silently.
- **Why it's harmful:** Suppresses issues, leaving the system in an inconsistent state and making debugging impossible.
- **What to do instead:** Let exceptions bubble up to the global middleware, or log the exception explicitly before throw.

---

## Examples

### ✅ Correct (C# 13 Primary Constructor, Collection Expression, Clean Architecture DTO)

**Application Service Interface (`src/Restaurant.Application/Services/IOrderWriteService.cs`):**
```csharp
namespace Restaurant.Application.Services;

public interface IOrderWriteService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken);
}
```

**Application Service Implementation with Primary Constructor (`src/Restaurant.Application/Services/OrderWriteService.cs`):**
```csharp
using Restaurant.Domain.Entities;
using Restaurant.Domain.Interfaces;

namespace Restaurant.Application.Services;

// C# 13 Primary Constructor injecting dependencies
public class OrderWriteService(
    IOrderRepository orderRepository,
    IMenuRepository menuRepository,
    IResourceLocalizer localizer) : IOrderWriteService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken)
    {
        // Fluent collection expression C# 13 syntax
        List<int> itemIds = [.. dto.Items.Select(i => i.MenuItemId)];
        
        var menuItems = await menuRepository.GetByIdsAsync(itemIds, cancellationToken);
        if (menuItems.Count != itemIds.Count)
        {
            // Resource file localization key (no hardcoded string)
            throw new BusinessException(localizer.Get("ErrorMenuItemNotFound"));
        }

        var order = Order.Create(dto.TableId);
        foreach (var item in dto.Items)
        {
            var menu = menuItems.First(m => m.Id == item.MenuItemId);
            order.AddItem(menu, item.Quantity);
        }

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);

        return new OrderDto(order.Id, order.TableId, order.TotalAmount, order.Status.ToString());
    }
}
```

**WebApi Controller with Primary Constructor (`src/Restaurant.WebApi/Controllers/OrdersController.cs`):**
```csharp
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Services;

namespace Restaurant.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController(IOrderWriteService orderWriteService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateAsync(
        [FromBody] CreateOrderDto dto, 
        CancellationToken cancellationToken)
    {
        // Thin controller delegates work, passes token
        var result = await orderWriteService.CreateOrderAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(CreateAsync), new { id = result.Id }, result);
    }
}
```

### ❌ Incorrect (Legacy constructor, inline DB queries, hardcoded errors)

```csharp
// Legacy namespace nesting, raw constructor boilerplate
namespace Restaurant.WebApi.Controllers
{
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        // Legacy DI structure
        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [Route("api/v1/orders/create-new-order")] // Verb in URL, violates API-001
        public IActionResult CreateOrder(int tableId, [FromBody] List<OrderItem> items)
        {
            // ❌ Business logic and validation inside controller
            if (tableId <= 0)
            {
                return BadRequest("Mã bàn không hợp lệ"); // ❌ Hardcoded string
            }

            var order = new Order { TableId = tableId, CreatedAt = DateTime.Now };
            _db.Orders.Add(order);
            _db.SaveChanges(); // ❌ Synchronous DB call, no cancellation token

            return Ok(order); // ❌ Exposes database entity directly
        }
    }
}
```

---

## Checklist
- [ ] Is the project split into Domain, Application, Infrastructure, and WebApi with proper dependencies?
- [ ] Does the class leverage Primary Constructors for constructor injection?
- [ ] Are all collection instances defined using modern collection expressions `[]`?
- [ ] Are controllers completely thin, containing zero business rules?
- [ ] Do all asynchronous actions accept and propagate `CancellationToken` inputs?
- [ ] Are services split into role-specific read and write interfaces?
- [ ] Is exception handling centralized in a global middleware returning standard envelopes?
- [ ] Are all user-facing strings, validation text, and error notifications localized?
- [ ] Are all log events created using structured arguments via `ILogger`?
