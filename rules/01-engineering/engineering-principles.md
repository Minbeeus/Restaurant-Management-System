# Engineering Principles

> **Rule Hierarchy Level:** 01
> **Applies To:** All
> **Last Updated:** 2025-07-14

## Purpose

This is the **highest-level document** in the Rule System. It defines fundamental engineering principles that every team member and every automated agent MUST follow. All other convention documents in this hierarchy derive authority from — and MUST align with — the principles stated here. When a lower-level convention conflicts with this document, this document wins.

## Scope

This document covers **universal engineering principles** applicable to all layers of the system: backend, frontend, mobile, database, infrastructure, and tooling.

It does **NOT** cover:
- Specific architecture layer rules — see *02 Architecture Conventions*.
- Coding style and formatting — see *03 Code Style Conventions*.
- Testing strategies — see *06 Testing Conventions*.
- UI/UX component design — see *08 UI Component Conventions*.

## Principles

1. **Correctness Over Cleverness** — Code MUST be correct and readable before it is clever or optimized. A straightforward implementation that is easy to verify is always preferred over a "smart" one that is hard to reason about.
2. **Explicit Over Implicit** — Dependencies, behavior, and side effects MUST be made explicit. Hidden coupling, ambient state, and magic behavior create maintenance nightmares.
3. **Single Source of Truth** — Every piece of business knowledge, configuration value, and user-facing string MUST have exactly one canonical location. Duplication is a liability.
4. **Design for Change** — Software entities MUST be structured so that anticipated changes require addition of new code, not modification of existing, tested code.
5. **Reuse Before Reinvention** — Before creating any new class, component, or service, developers MUST search the existing codebase for reusable assets. Duplication is the last resort.

---

## Mandatory Rules

### SOLID Principles

#### ENG-001: Single Responsibility Principle (SRP)

**Rule:** Every class, module, and function MUST have exactly **one reason to change** — meaning it serves one actor or one business capability.

**Rationale:** Classes with multiple responsibilities become fragile. A change for one stakeholder can break functionality required by another. SRP keeps blast radius small and makes code easier to test.

**Consequence:** Classes violating SRP MUST be refactored into focused collaborators before the pull request is merged. God classes (classes exceeding ~200 lines or serving multiple unrelated concerns) MUST be split.

```csharp
// ❌ INCORRECT — One class handles formatting, persistence, AND notification
public class OrderService
{
    public string FormatOrderReceipt(Order order) { /* ... */ }
    public void SaveToDatabase(Order order) { /* ... */ }
    public void SendEmailNotification(Order order) { /* ... */ }
}

// ✅ CORRECT — Each class has a single, focused responsibility
public class OrderReceiptFormatter
{
    public string Format(Order order) { /* ... */ }
}

public class OrderRepository : IOrderRepository
{
    public Task SaveAsync(Order order) { /* ... */ }
}

public class OrderNotificationService : IOrderNotificationService
{
    public Task NotifyAsync(Order order) { /* ... */ }
}
```

---

#### ENG-002: Open/Closed Principle (OCP)

**Rule:** Software entities (classes, modules, functions) MUST be **open for extension** but **closed for modification**. New behavior MUST be added through new code (new classes, new implementations), not by editing existing, tested code.

**Rationale:** Modifying existing code risks introducing regressions in already-tested behavior. Extension through abstraction (strategy pattern, plugin architecture, decorator pattern) keeps the existing codebase stable.

**Consequence:** If adding a new feature requires modifying an existing class's core logic (beyond simple configuration), the design MUST be refactored to use an extensibility mechanism first.

```csharp
// ❌ INCORRECT — Adding a new discount type forces modification of existing code
public class DiscountCalculator
{
    public decimal Calculate(Order order, string discountType)
    {
        if (discountType == "percentage")
            return order.Total * 0.1m;
        else if (discountType == "fixed")
            return 5.0m;
        // Every new discount type modifies this class
    }
}

// ✅ CORRECT — New discount types are added by implementing a new strategy
public interface IDiscountStrategy
{
    decimal Calculate(Order order);
}

public class PercentageDiscount : IDiscountStrategy
{
    private readonly decimal _rate;
    public PercentageDiscount(decimal rate) => _rate = rate;
    public decimal Calculate(Order order) => order.Total * _rate;
}

public class FixedDiscount : IDiscountStrategy
{
    private readonly decimal _amount;
    public FixedDiscount(decimal amount) => _amount = amount;
    public decimal Calculate(Order order) => _amount;
}

// Adding "BuyOneGetOneFree" = new class, zero modification to existing code
```

---

#### ENG-003: Liskov Substitution Principle (LSP)

**Rule:** Subtypes MUST be fully substitutable for their base types without altering the correctness of the program. Derived classes MUST NOT violate the behavioral contract (preconditions, postconditions, invariants) established by the base type.

**Rationale:** If a subtype cannot honor the contract of its parent, polymorphism breaks. Callers relying on the base type's guarantees will encounter unexpected exceptions, silent data corruption, or logic errors.

**Consequence:** Any derived class that throws `NotSupportedException` for inherited methods, tightens preconditions, or weakens postconditions MUST be redesigned — typically by splitting the base abstraction.

```csharp
// ❌ INCORRECT — Square violates Rectangle's behavioral contract
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }
    public int Area => Width * Height;
}

public class Square : Rectangle
{
    public override int Width
    {
        set { base.Width = value; base.Height = value; } // Surprise!
    }
    public override int Height
    {
        set { base.Width = value; base.Height = value; } // Surprise!
    }
}
// Caller sets Width=5, Height=10, expects Area=50, gets Area=100

// ✅ CORRECT — Use a common abstraction without inheritance coupling
public interface IShape
{
    int Area { get; }
}

public class Rectangle : IShape
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Area => Width * Height;
}

public class Square : IShape
{
    public int Side { get; set; }
    public int Area => Side * Side;
}
```

---

#### ENG-004: Interface Segregation Principle (ISP)

**Rule:** Clients MUST NOT be forced to depend on interface members they do not use. Large interfaces MUST be split into smaller, role-specific interfaces.

**Rationale:** Fat interfaces create unnecessary coupling. A class that only needs to read orders should not take a dependency that also exposes delete capabilities. Segregated interfaces make the dependency graph honest and testable.

**Consequence:** Interfaces with more than ~7 members MUST be reviewed for splitting. Any interface method that forces implementors to write `throw new NotSupportedException()` signals an ISP violation and MUST be refactored.

```csharp
// ❌ INCORRECT — Fat interface forces implementors to support everything
public interface IOrderService
{
    Task<Order> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> SearchAsync(OrderFilter filter);
    Task<Order> CreateAsync(CreateOrderCommand command);
    Task UpdateAsync(UpdateOrderCommand command);
    Task DeleteAsync(int id);
    Task<byte[]> ExportToPdfAsync(int id);
    Task SendToKitchenAsync(int id);
}

// ✅ CORRECT — Split into focused, role-specific interfaces
public interface IOrderReadService
{
    Task<Order> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> SearchAsync(OrderFilter filter);
}

public interface IOrderWriteService
{
    Task<Order> CreateAsync(CreateOrderCommand command);
    Task UpdateAsync(UpdateOrderCommand command);
    Task DeleteAsync(int id);
}

public interface IOrderExportService
{
    Task<byte[]> ExportToPdfAsync(int id);
}

public interface IKitchenDispatchService
{
    Task SendToKitchenAsync(int id);
}
```

---

#### ENG-005: Dependency Inversion Principle (DIP)

**Rule:** High-level modules MUST NOT depend on low-level modules. Both MUST depend on abstractions. Abstractions MUST NOT depend on details; details MUST depend on abstractions.

**Rationale:** When business logic directly references infrastructure (e.g., a specific database client, a particular email SDK), it becomes impossible to test in isolation and painful to swap implementations. DIP ensures the dependency arrow always points inward — toward the domain.

**Consequence:** Any direct `new` instantiation of infrastructure services inside business logic classes MUST be replaced with constructor-injected abstractions. Violations discovered during code review MUST be resolved before merge.

```csharp
// ❌ INCORRECT — High-level service depends directly on low-level implementation
public class OrderProcessor
{
    private readonly SqlOrderRepository _repo = new SqlOrderRepository();
    private readonly SmtpEmailSender _email = new SmtpEmailSender();

    public async Task ProcessAsync(Order order)
    {
        await _repo.SaveAsync(order);
        await _email.SendAsync(order.CustomerEmail, "Order confirmed");
    }
}

// ✅ CORRECT — Depend on abstractions, inject via constructor
public class OrderProcessor
{
    private readonly IOrderRepository _repo;
    private readonly IEmailSender _email;

    public OrderProcessor(IOrderRepository repo, IEmailSender email)
    {
        _repo = repo;
        _email = email;
    }

    public async Task ProcessAsync(Order order)
    {
        await _repo.SaveAsync(order);
        await _email.SendAsync(order.CustomerEmail, "Order confirmed");
    }
}
```

---

### General Engineering Rules

#### ENG-006: DRY — Don't Repeat Yourself

**Rule:** Every piece of knowledge MUST have a single, unambiguous, authoritative representation within the system. Duplicated logic MUST be extracted into shared abstractions.

**Rationale:** Duplicated knowledge means duplicated bugs and inconsistent behavior when one copy is updated but others are forgotten.

**Consequence:** Duplicated business logic discovered during review MUST be consolidated. However, beware of **wrong DRY** — coupling unrelated things that happen to look similar today but change for different reasons is worse than duplication.

```csharp
// ❌ INCORRECT — Tax calculation duplicated in two places
public class InvoiceService
{
    public decimal GetTotal(decimal subtotal) => subtotal * 1.10m; // 10% tax
}

public class ReceiptService
{
    public decimal GetTotal(decimal subtotal) => subtotal * 1.10m; // 10% tax
}

// ✅ CORRECT — Single source of truth for tax calculation
public class TaxCalculator : ITaxCalculator
{
    public decimal ApplyTax(decimal subtotal, TaxRate rate)
        => subtotal * (1 + rate.Value);
}
```

> **⚠ Warning — Wrong DRY:** Two functions that happen to have similar code but serve different business contexts (e.g., `CalculateShippingWeight` and `CalculateNutritionWeight`) SHOULD NOT be merged. They change for different reasons. Coupling them creates fragility.

---

#### ENG-007: KISS — Keep It Simple

**Rule:** Every solution MUST use the simplest approach that satisfies current requirements. Over-engineering, premature abstraction, and speculative generality MUST NOT be introduced.

**Rationale:** Complex code is expensive to read, debug, and maintain. Complexity MUST be justified by concrete, present-day requirements — not hypothetical future needs.

**Consequence:** Overly abstract designs with no current justification MUST be simplified during review. A reviewer MAY request simplification even if the complex code is technically correct.

```csharp
// ❌ INCORRECT — Over-engineered for a simple lookup
public class MenuItemPriceStrategyFactoryProvider
{
    public IMenuItemPriceStrategyFactory Create(MenuContext ctx) { /* ... */ }
}

// ✅ CORRECT — Simple and direct
public class MenuItemPriceCalculator
{
    public decimal GetPrice(MenuItem item, DateTime orderTime)
        => item.HasHappyHourPrice && IsHappyHour(orderTime)
            ? item.HappyHourPrice
            : item.RegularPrice;
}
```

---

#### ENG-008: YAGNI — You Aren't Gonna Need It

**Rule:** Developers MUST NOT implement functionality until it is actually needed by a current requirement. Speculative features, unused parameters, and "just-in-case" abstractions MUST NOT be added.

**Rationale:** Unused code is a maintenance burden. It must be read, understood, tested, and kept compatible during refactors — all for zero business value.

**Consequence:** Code added without a backing requirement or user story MUST be removed or deferred. Reviewers MUST challenge features that have no immediate consumer.

```csharp
// ❌ INCORRECT — Building export to 5 formats when only PDF is needed
public interface IExportService
{
    Task<byte[]> ExportToPdfAsync(int orderId);
    Task<byte[]> ExportToCsvAsync(int orderId);     // No requirement yet
    Task<byte[]> ExportToXmlAsync(int orderId);     // No requirement yet
    Task<byte[]> ExportToJsonAsync(int orderId);    // No requirement yet
    Task<byte[]> ExportToExcelAsync(int orderId);   // No requirement yet
}

// ✅ CORRECT — Build only what is needed now
public interface IOrderExportService
{
    Task<byte[]> ExportToPdfAsync(int orderId);
}
// When CSV is actually required, add it then — OCP makes this easy
```

---

#### ENG-009: Separation of Concerns

**Rule:** Each module, class, and layer MUST address a single, distinct concern. Presentation logic MUST NOT contain business rules. Business rules MUST NOT contain data-access details. Data-access code MUST NOT format user-facing output.

**Rationale:** Mixing concerns makes code untestable, unreusable, and fragile. A change to the UI should never risk breaking a business rule, and vice versa.

**Consequence:** Classes or methods that mix concerns across architectural layers MUST be refactored to respect layer boundaries as defined in *02 Architecture Conventions*.

```csharp
// ❌ INCORRECT — Blazor component contains SQL and business logic
@code {
    private async Task SubmitOrder()
    {
        if (order.Items.Count == 0) { errorMsg = "Cart is empty"; return; }
        var tax = order.Subtotal * 0.1m;
        await using var conn = new SqlConnection(connStr);
        await conn.ExecuteAsync("INSERT INTO Orders ...", order);
    }
}

// ✅ CORRECT — Each concern lives in its own layer
// Domain: Business rule
public class Order
{
    public Result Validate()
        => Items.Count == 0 ? Result.Failure(ErrorKeys.Order.CartEmpty) : Result.Success();
}

// Application: Orchestration
public class SubmitOrderHandler : IRequestHandler<SubmitOrderCommand, Result>
{
    public async Task<Result> Handle(SubmitOrderCommand cmd, CancellationToken ct)
    {
        var order = await _repo.GetAsync(cmd.OrderId, ct);
        var validation = order.Validate();
        if (!validation.IsSuccess) return validation;
        order.ApplyTax(_taxCalculator);
        await _repo.SaveAsync(order, ct);
        return Result.Success();
    }
}

// UI: Presentation only
@code {
    private async Task OnSubmitClicked()
    {
        var result = await OrderService.SubmitAsync(currentOrder);
        if (!result.IsSuccess) ShowError(result.ErrorKey);
    }
}
```

---

#### ENG-010: No Hardcoded Text

**Rule:** ALL user-facing strings — including UI labels, error messages, validation messages, toast notifications, email subjects, and confirmation dialogs — MUST be externalized to resource files, constants classes, or a localization system. No magic strings in logic code.

**Rationale:** Hardcoded strings make localization impossible, create inconsistency when the same message appears in multiple places, and make global text changes expensive and error-prone.

**Consequence:** Any hardcoded user-facing string found during code review MUST be moved to the appropriate resource file before merge. This rule applies to both backend and frontend code.

```csharp
// ❌ INCORRECT — Hardcoded strings scattered through code
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.TableNumber)
            .NotEmpty().WithMessage("Table number is required");
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must have at least one item");
    }
}

// ✅ CORRECT — Messages externalized to resource/constants
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator(IStringLocalizer<ValidationMessages> localizer)
    {
        RuleFor(x => x.TableNumber)
            .NotEmpty().WithMessage(localizer[ValidationKeys.TableNumberRequired]);
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage(localizer[ValidationKeys.OrderMustHaveItems]);
    }
}

// Or using a constants class when full localization is not yet needed:
public static class ValidationMessages
{
    public const string TableNumberRequired = "Table number is required";
    public const string OrderMustHaveItems = "Order must have at least one item";
}
```

```html
<!-- ❌ INCORRECT — Hardcoded text in Blazor component -->
<button class="btn btn-primary">Submit Order</button>
<p class="error">Something went wrong. Please try again.</p>

<!-- ✅ CORRECT — Text sourced from resources -->
<button class="btn btn-primary">@Loc["Button_SubmitOrder"]</button>
<p class="error">@Loc["Error_GenericRetry"]</p>
```

---

#### ENG-011: Mandatory Component and Service Reuse

**Rule:** Before creating any new class, component, service, or utility, developers MUST search the existing codebase for a reusable asset that satisfies the requirement. New creation is permitted only when no suitable existing asset is found or when the existing asset would require a modification that violates OCP.

**Rationale:** Duplicate components lead to inconsistent behavior, increased bundle size, divergent bug fixes, and wasted development effort. Reuse compounds quality.

**Consequence:** Pull requests introducing a new component or service that duplicates an existing one MUST be rejected. The reviewer MUST point to the existing asset and request reuse.

```
// ❌ INCORRECT — Creating a second confirmation dialog component
Components/
  ConfirmDialog.razor         ← Already exists
  OrderConfirmDialog.razor    ← Duplicate! Only changes the title text

// ✅ CORRECT — Reuse the existing component with parameters
<ConfirmDialog
    Title="@Loc["Dialog_ConfirmOrder"]"
    Message="@Loc["Dialog_ConfirmOrderMessage"]"
    OnConfirm="HandleOrderConfirm" />
```

---

## Recommended Practices

#### ENG-012: Prefer Composition Over Inheritance

**Rule:** Developers SHOULD favor object composition and interface implementation over class inheritance for code reuse and polymorphism.

**Rationale:** Deep inheritance hierarchies create tight coupling and make reasoning about behavior difficult. Composition provides flexibility to mix behaviors at runtime without the fragile base class problem.

**Consequence:** Inheritance deeper than 2 levels SHOULD be reviewed and refactored into composition unless the hierarchy genuinely models an "is-a" relationship.

---

#### ENG-013: Fail Fast

**Rule:** Methods SHOULD validate preconditions at entry points and throw/return errors immediately rather than allowing invalid state to propagate.

**Rationale:** The further invalid data travels from its point of entry, the harder it is to diagnose the root cause. Early validation keeps error messages actionable and stack traces short.

**Consequence:** Methods that silently swallow invalid input SHOULD be refactored to validate and fail at the boundary.

```csharp
// ❌ NOT RECOMMENDED — Silent null propagation leads to NullReferenceException later
public async Task<OrderDto> GetOrderAsync(int? orderId)
{
    var order = await _repo.GetAsync(orderId.Value); // Crashes far from caller
    return _mapper.Map<OrderDto>(order);
}

// ✅ RECOMMENDED — Fail fast with clear message
public async Task<OrderDto> GetOrderAsync(int? orderId)
{
    ArgumentNullException.ThrowIfNull(orderId);
    var order = await _repo.GetAsync(orderId.Value)
        ?? throw new NotFoundException(nameof(Order), orderId.Value);
    return _mapper.Map<OrderDto>(order);
}
```

---

#### ENG-014: Favor Immutability

**Rule:** Data structures SHOULD be immutable by default. Use `record`, `readonly`, `init`, and immutable collections where possible.

**Rationale:** Immutable objects are inherently thread-safe, easier to reason about, and eliminate an entire category of bugs caused by unexpected mutation.

**Consequence:** Mutable classes used as DTOs or value objects SHOULD be converted to records or read-only structures.

---

#### ENG-015: Self-Documenting Code

**Rule:** Code SHOULD be written so that its intent is clear from names, structure, and types — minimizing the need for comments. Comments SHOULD explain **why**, not **what**.

**Rationale:** Comments describing what code does tend to become stale. Meaningful names and small methods communicate intent more reliably than prose.

**Consequence:** Methods with cryptic names or excessive inline comments SHOULD be refactored for clarity.

```csharp
// ❌ NOT RECOMMENDED — Comment explains what, name is cryptic
// Check if the order can be modified
public bool Chk(Order o) => o.St != 3 && o.St != 4;

// ✅ RECOMMENDED — Name communicates intent, no comment needed
public bool CanBeModified(Order order)
    => order.Status is not (OrderStatus.Completed or OrderStatus.Cancelled);
```

---

## Anti-patterns

### AP-01: God Class

**Description:** A single class that handles multiple unrelated responsibilities — data access, business logic, formatting, and notifications.

**Why It's Harmful:** Impossible to test in isolation, high merge-conflict frequency, a single change can break unrelated features.

**What To Do Instead:** Apply SRP (ENG-001). Extract each responsibility into its own focused class with a clear interface.

---

### AP-02: Service Locator

**Description:** Resolving dependencies by calling a global container (`ServiceLocator.Get<T>()`) instead of receiving them via constructor injection.

**Why It's Harmful:** Hides dependencies, makes classes hard to test, and violates DIP (ENG-005). The class's constructor no longer tells the truth about what it needs.

**What To Do Instead:** Use constructor injection exclusively. Let the DI container compose the object graph at the composition root.

---

### AP-03: Premature Abstraction

**Description:** Creating interfaces, base classes, and factories before there is a second use case, "just in case."

**Why It's Harmful:** Violates YAGNI (ENG-008) and KISS (ENG-007). Adds cognitive load, increases file count, and the abstraction often doesn't fit when the real second use case arrives.

**What To Do Instead:** Write concrete implementations first. Extract abstractions when a genuine second use case demands it (Rule of Three).

---

### AP-04: Copy-Paste Driven Development

**Description:** Duplicating existing code blocks to create similar features instead of extracting shared logic.

**Why It's Harmful:** Violates DRY (ENG-006). Bug fixes must be applied N times. Behavior diverges silently over time.

**What To Do Instead:** Extract shared logic into a common service, base class, or utility. Use parameterization for variation.

---

### AP-05: Hardcoded Magic Strings

**Description:** Embedding user-facing text, connection strings, feature flags, or configuration values directly in source code.

**Why It's Harmful:** Violates ENG-010. Makes localization impossible, creates hidden dependencies, and turns simple text changes into code deployments.

**What To Do Instead:** Externalize all user-facing strings to resource files or constants. Move configuration to `appsettings.json` or environment variables.

---

## Checklist

Use this checklist for self-verification before submitting any pull request:

### SOLID Compliance
- □ Each new class has exactly one reason to change (SRP — ENG-001)
- □ New behavior is added via new classes/implementations, not by modifying existing ones (OCP — ENG-002)
- □ Derived classes honor all contracts of their base types (LSP — ENG-003)
- □ No interface forces implementors to stub out methods with `NotSupportedException` (ISP — ENG-004)
- □ Business logic depends on abstractions, not concrete infrastructure classes (DIP — ENG-005)

### General Principles
- □ No duplicated business logic — shared logic is extracted to a single source (DRY — ENG-006)
- □ Solution uses the simplest approach that meets current requirements (KISS — ENG-007)
- □ No speculative features without a backing requirement (YAGNI — ENG-008)
- □ Presentation, business, and data-access concerns are in separate layers (SoC — ENG-009)
- □ Zero hardcoded user-facing strings in logic or UI code (ENG-010)
- □ Existing components/services were searched before creating new ones (ENG-011)
- □ Composition is preferred over deep inheritance hierarchies (ENG-012)
- □ Methods validate preconditions at entry and fail fast (ENG-013)
- □ Code reads like prose — names reveal intent, comments explain **why** (ENG-015)

---

*This document is the authoritative reference for engineering principles. All other conventions in the Rule System MUST be consistent with the rules defined here. When in doubt, default to simplicity, explicitness, and correctness.*
