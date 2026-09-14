# Coding Convention

> **Rule Hierarchy Level:** 03
> **Applies To:** All
> **Last Updated:** 2025-07-14

## Purpose

This document establishes the coding standards that every developer and AI agent MUST follow when writing source code across all layers of the system. It covers naming, formatting, method and class design, exception handling, the no-hardcoded-text policy, and documentation expectations. Consistent adherence to these rules produces a codebase that is readable, maintainable, and ready for localization.

## Scope

This convention covers the structure and style of **source code files** — how identifiers are named, how code is formatted, how methods and classes are designed, how exceptions are handled, how user-facing text is externalized, and how code is documented.

**Out of scope** (covered elsewhere):
- Project architecture and layer responsibilities → *Architecture Convention*
- REST API endpoint design and response format → *API Convention*
- Database schema and migration rules → *Database Convention*
- UI component structure and CSS methodology → *UI Convention*
- Security, authentication, and authorization → *Security Convention*
- Testing strategy and coverage requirements → *Testing Convention*

## Principles

1. **Readability over cleverness** — Code is read far more often than it is written. Every naming choice, formatting decision, and structural pattern MUST favor the reader's comprehension over the writer's convenience.
2. **SOLID as the foundation** — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion are not aspirational goals; they are mandatory constraints on every class and method.
3. **Zero hardcoded user-facing text** — Any string that a human (end-user, operator, or developer reading logs) will ever see MUST be externalized. This enables localization, centralizes message management, and eliminates scattered magic strings.
4. **Self-documenting code first, comments second** — Well-chosen names and small, focused methods eliminate the need for most comments. When a comment is necessary, it explains *why*, never *what*.
5. **Reuse before creation** — Before writing a new helper, utility, or abstraction, search the codebase for an existing one. Duplication is a defect.

---

## Mandatory Rules

### Naming Conventions

**CODE-001: PascalCase for public and protected members**
All public and protected classes, methods, properties, events, enums, and delegates MUST use PascalCase.
- **Rationale:** Consistent casing communicates visibility at a glance and aligns with .NET framework conventions.
- **Consequence:** PR/code review rejection; lint tool enforcement failure.

**CODE-002: camelCase for local variables and parameters**
Local variables and method parameters MUST use camelCase.
- **Rationale:** Differentiates local scope from class-level members without extra cognitive load.
- **Consequence:** PR/code review rejection.

**CODE-003: Private fields with underscore prefix**
Private instance fields MUST use `_camelCase` (leading underscore followed by camelCase). Static private fields follow the same rule.
- **Rationale:** Distinguishes fields from local variables and parameters instantly, preventing accidental shadowing.
- **Consequence:** Naming inconsistency; potential bugs from field-parameter confusion.

**CODE-004: Interface prefix with 'I'**
All interface types MUST be prefixed with the uppercase letter `I`.
- **Rationale:** Immediately identifies abstractions in constructor signatures, DI registrations, and variable declarations.
- **Consequence:** Tooling and convention misalignment; reader confusion about whether a type is concrete.

**CODE-005: Constants in UPPER_CASE**
Compile-time constants (`const`) and static readonly fields acting as logical constants MUST use UPPER_SNAKE_CASE.
- **Rationale:** Constants stand out from regular fields, signaling that values are fixed and safe to inline.
- **Consequence:** Constants become indistinguishable from mutable fields.

**CODE-006: Boolean names MUST read as questions**
Boolean variables, parameters, properties, and method return values MUST be named as true/false questions using prefixes such as `is`, `has`, `can`, `should`, `was`, or `allows`.
- **Rationale:** `if (isActive)` is immediately understandable; `if (active)` is ambiguous.
- **Consequence:** Reduced readability and potential logical misinterpretation.

**CODE-007: Meaningful, self-documenting names — no abbreviations**
All identifiers MUST use complete, descriptive English words. Abbreviations MUST NOT be used unless they are universally understood domain terms (e.g., `Id`, `Url`, `Http`, `Dto`).
- **Rationale:** `remainingQuantity` is instantly clear; `remQty` requires decoding.
- **Consequence:** Increased onboarding time; misunderstanding of purpose.

**CODE-008: Async method suffix**
All methods that return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>` MUST be suffixed with `Async`.
- **Rationale:** Callers immediately know to `await` the result, preventing deadlocks and fire-and-forget bugs.
- **Consequence:** Inconsistent API surface; risk of synchronous calls on async methods.

### Formatting

**CODE-009: Indentation MUST use 4 spaces**
All source files MUST use 4-space indentation. Tab characters MUST NOT be used.
- **Rationale:** Consistent rendering across all editors, diff tools, and code review platforms.
- **Consequence:** Mixed indentation causes visual noise in diffs and reviews.

**CODE-010: Maximum line length of 120 characters**
Lines MUST NOT exceed 120 characters. Break long lines at logical points (after commas, before operators).
- **Rationale:** Side-by-side diff views and standard monitors display ~120 columns comfortably.
- **Consequence:** Horizontal scrolling disrupts code review flow.

**CODE-011: Allman bracing style**
Opening braces MUST be placed on a new line, aligned with the parent statement. This applies to classes, methods, properties, control flow statements, and lambdas exceeding one line.
- **Rationale:** Allman style creates clear visual blocks, improving scanability of nested structures.
- **Consequence:** Inconsistent bracing style across the codebase.

**CODE-012: File organization order**
Source files MUST organize members in the following order: (1) `using` directives, (2) namespace declaration, (3) class/struct/interface declaration, and within the type: (a) constants, (b) static fields, (c) instance fields, (d) constructors, (e) public properties, (f) public methods, (g) internal/protected methods, (h) private methods.
- **Rationale:** Predictable structure lets reviewers and AI agents locate code quickly.
- **Consequence:** Wasted time hunting for members; inconsistent patterns across files.

**CODE-013: Blank lines for logical separation**
A single blank line MUST separate method definitions, property groups, and logical sections within a method. Two consecutive blank lines MUST NOT appear.
- **Rationale:** Blank lines guide the eye; excessive blank lines waste vertical space.
- **Consequence:** Visual clutter or a wall of undifferentiated code.

### Method Design

**CODE-014: Single responsibility per method**
Each method MUST perform exactly one logical task. If a method requires a comment to separate "phases", those phases MUST be extracted into separate methods.
- **Rationale:** SRP at the method level makes code testable, reusable, and easy to name.
- **Consequence:** Long methods become untestable monoliths; refactoring becomes risky.

**CODE-015: Maximum 4 parameters per method**
Methods MUST NOT accept more than 4 parameters. When more data is needed, group related parameters into a request object, DTO, or value object.
- **Rationale:** Many parameters make call sites unreadable and increase the chance of argument-order bugs.
- **Consequence:** Fragile call sites; cognitive overload for callers.

**CODE-016: Guard clauses over nested conditions**
Methods MUST validate preconditions at the top using guard clauses (early `return`, `throw`) rather than wrapping the entire method body in `if` blocks.
- **Rationale:** Guard clauses flatten the method, reducing indentation and making the happy path obvious.
- **Consequence:** Deeply nested code that is hard to follow and test.

**CODE-017: Explicit return types**
Methods MUST declare explicit return types. The use of `dynamic` as a return type MUST NOT appear in production code.
- **Rationale:** Explicit types enable compile-time checking and IntelliSense documentation.
- **Consequence:** Runtime failures and loss of static analysis benefits.

### Class Design

**CODE-018: Single responsibility per class**
Each class MUST have one — and only one — reason to change. If a class name requires the word "And" or handles more than one business concept, it MUST be split.
- **Rationale:** SRP is the foundation of maintainability and testability.
- **Consequence:** God classes that are impossible to test in isolation and painful to modify.

**CODE-019: Maximum class size of 300 lines**
A class file SHOULD NOT exceed 300 lines of code (excluding blank lines and comments). Exceeding 400 lines MUST trigger a refactoring review.
- **Rationale:** Large classes are a symptom of SRP violation and discourage comprehensive reading.
- **Consequence:** Sprawling files that developers avoid modifying out of fear.

**CODE-020: Composition over inheritance**
Classes MUST favor composition (injecting collaborators via constructor) over inheritance. Inheritance MUST only be used for genuine "is-a" relationships, not for code reuse.
- **Rationale:** Composition provides flexibility, testability (mocking), and avoids fragile base class problems.
- **Consequence:** Deep inheritance hierarchies that resist change and break Liskov Substitution.

**CODE-021: Constructor injection for dependencies**
All dependencies MUST be injected through the constructor and stored in `private readonly` fields. Service Locator and `new`-ing up services MUST NOT be used.
- **Rationale:** Constructor injection makes dependencies explicit, immutable, and testable.
- **Consequence:** Hidden dependencies; impossible to unit test without the DI container.

### Exception Handling

**CODE-022: Catch specific exceptions**
Code MUST catch the most specific exception type possible. Catching bare `Exception` or `SystemException` MUST NOT occur unless the purpose is to log and re-throw at a global boundary.
- **Rationale:** Catching broad exceptions masks programming errors (e.g., `NullReferenceException`) and makes debugging harder.
- **Consequence:** Silent swallowing of critical bugs; incorrect error recovery.

**CODE-023: Never swallow exceptions**
Every `catch` block MUST either (a) re-throw the exception, (b) throw a new exception wrapping the original, (c) log the exception with full stack trace, or (d) return a meaningful error result. An empty `catch` block MUST NOT exist.
- **Rationale:** Swallowed exceptions create invisible failures that corrupt data silently.
- **Consequence:** Undiagnosable production issues; data integrity loss.

**CODE-024: Global exception handling for unhandled cases**
The application MUST register a global exception handler (middleware in ASP.NET Core, global handler in Blazor) that catches any unhandled exception, logs it with a correlation/trace ID, and returns a standardized error response — as specified in the *API Convention*.
- **Rationale:** Unhandled exceptions MUST NOT leak stack traces or internal details to clients.
- **Consequence:** Information disclosure vulnerability; poor user experience with raw error pages.

**CODE-025: Error messages from resource files**
All exception messages, validation messages, and error descriptions shown to users MUST be loaded from resource files (`.resx`), constants classes, or a localization service. Messages MUST NOT be hardcoded inline.
- **Rationale:** Centralizes message management and enables localization without code changes.
- **Consequence:** Scattered, inconsistent messages; impossible to translate; violation of CODE-030.

**CODE-026: Custom exception types for domain errors**
Domain-specific error conditions MUST be represented by custom exception classes inheriting from a base `DomainException`. Generic framework exceptions (e.g., `InvalidOperationException`) MUST NOT be used to communicate business rule violations.
- **Rationale:** Custom exceptions carry semantic meaning, enabling typed catch blocks and cleaner error handling.
- **Consequence:** Business errors are indistinguishable from infrastructure failures.

### No Hardcoded Text (Critical)

**CODE-030: All user-facing strings MUST be externalized**
Every string literal that represents a message, label, error description, notification text, tooltip, or any content a human will read MUST be defined in a resource file (`.resx`), a centralized constants class, or a localization system. This includes API response messages, validation error texts, log messages intended for operators, and UI text.
- **Rationale:** Hardcoded strings scatter translatable content, create inconsistency, resist search-and-replace, and block localization.
- **Consequence:** Immediate rejection at code review. No exception.

**CODE-031: No magic numbers**
Numeric literals MUST NOT appear in logic code. Define named constants, enums, or configuration values instead. The only permitted literals are `0`, `1`, `-1` in trivial contexts (loop initialization, increment, comparison to empty).
- **Rationale:** `if (status == 3)` is meaningless; `if (status == OrderStatus.Completed)` is self-documenting.
- **Consequence:** Unreadable code; bugs from misremembered numeric values.

**CODE-032: Technical strings are the only permitted inline literals**
Only the following string categories MAY remain inline: regex patterns, format specifiers without user text (e.g., `"yyyy-MM-dd"`), dictionary keys used internally, and configuration section names.
- **Rationale:** These strings are structural, not translatable, and benefit from proximity to their usage.
- **Consequence:** Over-externalizing technical strings adds indirection without value.

### Comments & Documentation

**CODE-040: Code MUST be self-documenting**
Developers MUST choose names, structures, and patterns that make the code's purpose obvious without comments. If a code block requires a comment to explain *what* it does, it MUST be refactored until the comment is unnecessary.
- **Rationale:** Comments explaining *what* drift out of sync with code; well-named methods never do.
- **Consequence:** Stale comments that mislead rather than help.

**CODE-041: Comments explain WHY, not WHAT**
When a comment is necessary, it MUST explain the *reason* behind a decision — a business rule, a workaround, a non-obvious constraint — not the mechanics of the code.
- **Rationale:** *Why* comments add information that the code cannot express; *what* comments duplicate it.
- **Consequence:** Noise without value; false sense of documentation.

**CODE-042: XML documentation for public APIs**
All public classes, methods, properties, and interfaces MUST have XML documentation comments (`/// <summary>`, `/// <param>`, `/// <returns>`). Internal and private members SHOULD have XML docs if their purpose is non-obvious.
- **Rationale:** XML docs generate IntelliSense tooltips and API reference documentation automatically.
- **Consequence:** Consumers of the API must read source code to understand method contracts.

**CODE-043: TODO comments MUST reference a ticket**
`TODO` comments MUST include a tracking ticket reference (e.g., `// TODO [KDS-1234]: Implement retry logic`). Orphan TODOs without tickets MUST NOT be merged.
- **Rationale:** Untracked TODOs are effectively dead comments that never get addressed.
- **Consequence:** Accumulating technical debt without visibility.

---

## Recommended Practices

**CODE-050: Use `var` when the type is obvious**
Developers SHOULD use `var` when the right-hand side makes the type unambiguous (e.g., `var order = new Order();`). Explicit types SHOULD be used when the type is not obvious from context.
- **Rationale:** Reduces visual noise while maintaining readability.

**CODE-051: Prefer expression-bodied members for single-line logic**
Properties, methods, and operators that contain a single expression SHOULD use expression-body syntax (`=>`).
- **Rationale:** Compact form reduces boilerplate for trivial members.

**CODE-052: Use records for immutable data**
DTOs, value objects, and event payloads SHOULD be declared as `record` or `record struct` types.
- **Rationale:** Records provide value equality, immutability, and concise syntax out of the box.

**CODE-053: Keep constructor bodies minimal**
Constructors SHOULD only assign parameters to fields. Any initialization logic SHOULD be extracted to factory methods or initialization methods.
- **Rationale:** Complex constructors are hard to test and violate SRP.

**CODE-054: Prefer string interpolation over concatenation**
String building SHOULD use interpolation (`$"..."`) or `StringBuilder` for loops. String concatenation with `+` SHOULD NOT be used for multi-part strings.
- **Rationale:** Interpolation is more readable and less error-prone than concatenation.

---

## Anti-patterns

### God Class
- **Description:** A single class that handles dozens of unrelated responsibilities — e.g., a `RestaurantService` that manages orders, inventory, staff, and reports.
- **Why it's harmful:** Violates SRP and OCP; changes to any feature risk breaking all others; impossible to test in isolation.
- **What to do instead:** Split into focused classes (`OrderService`, `InventoryService`, `StaffService`) each with its own interface.

### Swallowed Exception
- **Description:** A `catch` block that does nothing — `catch (Exception) { }` — or logs a generic message and continues.
- **Why it's harmful:** Failures become invisible; corrupted state propagates silently; debugging becomes guesswork.
- **What to do instead:** Log with full stack trace and either re-throw, wrap in a domain exception, or return an error result.

### Hardcoded String Soup
- **Description:** String literals scattered throughout logic code — `"Order not found"`, `"Invalid quantity"`, `"Đơn hàng đã bị hủy"`.
- **Why it's harmful:** Impossible to localize; inconsistent wording across identical conditions; missed during search-and-replace.
- **What to do instead:** Define in `.resx` resource files and reference via generated resource classes: `Messages.OrderNotFound`.

### Primitive Obsession
- **Description:** Using `string`, `int`, or `decimal` everywhere instead of meaningful types — `string email` instead of `Email email`.
- **Why it's harmful:** No compile-time validation; easy to swap arguments; business rules leak everywhere.
- **What to do instead:** Create value objects or strong types that encapsulate validation.

### Boolean Trap
- **Description:** Methods with boolean parameters that are unreadable at the call site — `ProcessOrder(order, true, false, true)`.
- **Why it's harmful:** Call sites are opaque; readers must look up the method signature to understand behavior.
- **What to do instead:** Use enums, named arguments, or separate methods (`ProcessOrderWithReceipt`, `ProcessOrderSilently`).

---

## Examples

### Naming — ✅ Correct vs ❌ Incorrect
```csharp
// ✅ CORRECT: PascalCase class, I-prefix, _camelCase field, Async suffix, UPPER_CASE const
public class OrderProcessingService : IOrderProcessingService
{
    private readonly IOrderRepository _orderRepository;
    public const int MAX_ITEMS_PER_ORDER = 50;

    public async Task<OrderDto> GetOrderByIdAsync(int orderId)
    {
        bool isOrderValid = orderId > 0;
        bool hasPermission = await _orderRepository.CanAccessAsync(orderId);
        // ...
    }
}

// ❌ INCORRECT: Abbreviations, missing prefix, wrong casing, no Async suffix
public class OrdProcSvc : OrderProcessingService        // Abbreviation, inheritance misuse
{
    private IOrderRepository orderRepo;                  // Missing underscore
    public const int maxItems = 50;                      // Not UPPER_CASE
    public Task<OrderDto> GetOrderById(int id)           // No Async, vague param
    {
        bool valid = id > 0;                             // Not a question (isValid)
    }
}
```

### Guard Clauses — ✅ Correct vs ❌ Incorrect
```csharp
// ✅ CORRECT: Guard clauses at the top, flat structure, resource-based messages
public async Task<OrderDto> CancelOrderAsync(int orderId, string reason)
{
    if (orderId <= 0)
        throw new ArgumentException(Resources.Errors.InvalidOrderId, nameof(orderId));

    var order = await _orderRepository.GetByIdAsync(orderId)
        ?? throw new NotFoundException(Resources.Errors.OrderNotFound);

    if (!order.CanBeCancelled)
        throw new DomainException(Resources.Errors.OrderCannotBeCancelled);

    order.Cancel(reason);
    await _orderRepository.UpdateAsync(order);
    return order.ToDto();
}

// ❌ INCORRECT: Deep nesting, hardcoded string, silent null return
public async Task<OrderDto> CancelOrderAsync(int orderId, string reason)
{
    if (orderId > 0)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order != null)
        {
            if (order.CanBeCancelled)
            {
                order.Cancel(reason);
                await _orderRepository.UpdateAsync(order);
                return order.ToDto();
            }
            else { throw new Exception("Order cannot be cancelled"); }  // Hardcoded!
        }
    }
    return null;  // Silent failure
}
```

### Externalized Strings — ✅ Correct vs ❌ Incorrect
```csharp
// ✅ CORRECT: Message from .resx resource file
// Resources/Errors.resx → InsufficientStock = "Insufficient stock for item: {0}."
throw new DomainException(string.Format(Resources.Errors.InsufficientStock, item.ProductName));

// ❌ INCORRECT: Hardcoded user-facing message
throw new Exception("Insufficient stock for item: " + item.ProductName);
```

### Magic Numbers — ✅ Correct vs ❌ Incorrect
```csharp
// ✅ CORRECT: Named constants
private const decimal LOYALTY_DISCOUNT_RATE = 0.10m;
private const int MINIMUM_LOYALTY_POINTS = 100;

if (customer.LoyaltyPoints >= MINIMUM_LOYALTY_POINTS)
    return total * LOYALTY_DISCOUNT_RATE;

// ❌ INCORRECT: Raw literals
if (customer.LoyaltyPoints >= 100)   // What is 100?
    return total * 0.10m;            // What is 0.10?
```

### Parameter Count — ✅ Correct vs ❌ Incorrect
```csharp
// ✅ CORRECT: Group into a request object
public record CreateOrderRequest(int TableId, int CustomerId,
    List<OrderItemDto> Items, string? VoucherCode);

public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request) { }

// ❌ INCORRECT: 6 loose parameters — unreadable call site
public async Task<OrderDto> CreateOrderAsync(
    int tableId, int customerId, List<OrderItemDto> items,
    string specialInstructions, string voucherCode, bool isPriority) { }
```

### Custom Domain Exception — ✅ Correct
```csharp
public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message) : base(message)
        => ErrorCode = errorCode;
}

public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public InsufficientStockException(int productId)
        : base(ErrorCodes.INSUFFICIENT_STOCK,
               string.Format(Resources.Errors.InsufficientStock, productId))
        => ProductId = productId;
}
```

---

## Checklist

### Naming
- □ All public members use PascalCase
- □ Local variables and parameters use camelCase
- □ Private fields use `_camelCase` prefix
- □ Interfaces are prefixed with `I`
- □ Constants use UPPER_SNAKE_CASE
- □ Booleans read as questions (`is`, `has`, `can`, `should`)
- □ No abbreviations in identifiers (except universally known: `Id`, `Dto`, `Url`)
- □ Async methods end with `Async`

### Formatting
- □ 4-space indentation, no tabs
- □ No line exceeds 120 characters
- □ Allman bracing style (opening brace on new line)
- □ File members ordered: constants → fields → constructors → properties → public methods → private methods
- □ Single blank line between methods; no double blank lines

### Method & Class Design
- □ Each method has a single, clearly named responsibility
- □ No method has more than 4 parameters
- □ Guard clauses at the top; no deep nesting
- □ Each class has a single reason to change
- □ No class exceeds 300 lines (review required at 400)
- □ Dependencies are constructor-injected into `private readonly` fields
- □ Composition is preferred over inheritance

### Exception Handling
- □ Only specific exception types are caught
- □ No empty `catch` blocks exist
- □ Global exception handler is registered and returns standardized errors
- □ Error messages come from resource files, not inline strings
- □ Domain errors use custom exception types inheriting from `DomainException`

### No Hardcoded Text
- □ Zero user-facing string literals in logic code
- □ All messages defined in `.resx` files or constants classes
- □ No magic numbers — all numeric constants are named
- □ Only technical strings (regex, date formats, config keys) remain inline

### Comments & Documentation
- □ Code is self-documenting — no *what* comments
- □ Any remaining comments explain *why*
- □ All public APIs have XML documentation (`/// <summary>`)
- □ Every `TODO` references a tracking ticket
