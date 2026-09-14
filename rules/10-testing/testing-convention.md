# Testing Conventions

> **Rule Hierarchy Level:** 10
> **Applies To:** All
> **Last Updated:** 2026-07-14

## Purpose
This document establishes the testing strategy, standards, and practices for verifying code correctness. It outlines requirements for unit testing patterns, integration testing, system/E2E testing, code coverage, mocking boundaries, and test data isolation.

## Scope
This convention covers unit tests, integration tests, E2E tests, mocking, and coverage rules. It does NOT cover performance budgets or load testing (see [Performance Convention](file:///d:/My%20Project/rules/12-performance/performance-convention.md)).

## Principles
1. **The Test Pyramid:** Maintain a balanced distribution of testing (many fast unit tests, fewer integration tests, and minimal critical path E2E tests).
2. **Deterministic Behavior:** Tests MUST be reliable, repeatable, and produce the same results regardless of execution order or environmental state.
3. **Behavior Verification:** Focus on testing business behaviors and public contracts, rather than verifying private implementation details.
4. **Arrange-Act-Assert:** Write tests with a clear, readable structure separating setups, executions, and validations.

---

## Mandatory Rules

### TEST-001: The Arrange-Act-Assert (AAA) Pattern
All automated tests MUST be structured using the **Arrange-Act-Assert** pattern. Each step MUST be demarcated, and only a single action (Act) should occur per test.
- **Rationale:** Standardizes test layout, making it immediately clear what is being prepared, what is being executed, and what is being asserted.
- **Consequence:** Conflating setups and assertions creates spaghetti test code that is difficult to debug when a failure occurs.

### TEST-002: Test Naming Standard
All unit and integration test method names MUST follow a consistent naming format reflecting the behavior:
- Format: `[MethodName]_[StateUnderTest]_[ExpectedBehavior]`
- Example: `CalculateTotal_WithActiveVoucher_AppliesDiscount`
- **Rationale:** Allows developers and build pipelines to understand precisely what failed and why without looking at the test source code.
- **Consequence:** Vague names (`TestMethod1`, `TestDelete`) require searching the code to understand what scenario failed.

### TEST-003: Database Test Isolation
Tests interacting with database logic MUST run against a clean database instance (such as an in-memory database, SQLite, or a transactional database state container) that is reset between tests. Tests MUST NOT share mutable data or depend on the execution order of other tests.
- **Rationale:** Shared database states cause intermittent test failures (flakiness) that are extremely difficult to diagnose.
- **Consequence:** Running tests that modify a shared database leads to cascade failures and developers ignoring CI/CD pipeline alerts.

### TEST-004: Mocking Boundaries
Tests MUST only mock interfaces and external integration points (such as third-party APIs, filesystem, or email senders) that are outside the system's control. Mocking internal domain entities, value objects, or business logic classes that can easily be instantiated is prohibited.
- **Rationale:** Over-mocking leads to fragile tests that pass even when the real application logic is broken because the mock simulates incorrect behavior.
- **Consequence:** Mocking every helper class defeats the purpose of validation and hides integration bugs.

### TEST-005: Zero Hardcoded Test Secrets
Tests requiring configuration values, endpoints, or API keys MUST NOT hardcode these values in the test source code. They MUST use environment configuration files or mock contexts.
- **Rationale:** Protects secret keys from being leaked into version control.
- **Consequence:** Security breaches occur when test configuration files containing production keys are pushed to git.

---

## Recommended Practices

### TEST-050: Target Code Coverage
The codebase SHOULD maintain a minimum of 80% code coverage on core domain logic and application services. High-priority calculations or security filters SHOULD target 100% coverage.
- **Rationale:** Ensures that critical business paths are guarded against regression.

### TEST-051: Stable Selectors for E2E Tests
End-to-End tests SHOULD use stable, dedicated testing attributes (such as `data-testid="submit-btn"`) instead of relying on fragile CSS layouts, tag names, or localized button labels.
- **Rationale:** Prevents UI redesigns (e.g., modifying CSS classes or updating local text) from breaking the E2E testing pipeline.

---

## Anti-patterns

### The Mock-Everything Test
Mocking the database, the repository, the validator, the mapper, and neighboring service classes, leaving only one line of actual code under test.
- **Why it's harmful:** Verifies nothing except that the mock library works. Fails to catch actual behavior bugs.
- **What to do instead:** Mock only true I/O boundaries. Use concrete application services, validators, and mappers in unit tests.

### Asserting Private Members
Using reflection or modifying access modifiers (`internal`, `public`) on helper classes simply to write a unit test for a private helper method.
- **Why it's harmful:** Tight-couples tests to implementation details, causing tests to break during harmless code refactoring.
- **What to do instead:** Test the public interface method that consumes the helper; if the helper is complex, extract it into a separate class with its own public interface.

---

## Examples

### ✅ Correct (AAA Structure, Naming, Interface Mocking)

**Unit Test (C# / xUnit Example):**
```csharp
[Fact]
public async Task CheckoutOrderAsync_ValidOrder_ProcessesPaymentAndReturnsSuccess()
{
    // Arrange (Set up test dependencies and inputs)
    var mockPaymentService = new Mock<IPaymentService>();
    var mockOrderRepository = new Mock<IOrderRepository>();
    
    var order = new Order { Id = 101, TotalAmount = 150000 };
    
    mockPaymentService
        .Setup(p => p.ProcessPaymentAsync(order.Id, order.TotalAmount, It.IsAny<CancellationToken>()))
        .ReturnsAsync(PaymentResult.Success());
        
    var service = new OrderProcessingService(mockOrderRepository.Object, mockPaymentService.Object);
    var cancellationToken = CancellationToken.None;

    // Act (Execute the single logic path)
    var result = await service.CheckoutOrderAsync(order, cancellationToken);

    // Assert (Verify the behavior and side effects)
    Assert.True(result.IsSuccess);
    mockOrderRepository.Verify(r => r.UpdateStatusAsync(order.Id, OrderStatus.Processing), Times.Once);
}
```

### ❌ Incorrect (No AAA structure, hardcoded database logic, vague naming)

```csharp
[Fact]
public void Test1()
{
    // Vague setup, mixed assertions, relies on a live database server state
    var db = new ApplicationDbContext("Server=live-prod-db;Database=ResturantDB;User Id=sa;Password=secret;");
    var service = new OrderService(db);
    
    var res = service.Create(new Order { Id = 1 });
    
    Assert.NotNull(res);
    // Modifies live database directly, causing downstream tests to fail when they see ID 1 already exists
}
```

---

## Checklist
- [ ] Are all test methods clearly structured with Arrange, Act, and Assert blocks?
- [ ] Does the test name clearly state the method name, state under test, and expected outcome?
- [ ] Are all database-dependent tests isolated using database-resets or in-memory engines?
- [ ] Are mocks limited to external services, APIs, and low-level I/O classes?
- [ ] Are there zero hardcoded API secrets or production database credentials in the test project?
- [ ] Do E2E test scripts locate elements using dedicated `data-testid` attributes?
