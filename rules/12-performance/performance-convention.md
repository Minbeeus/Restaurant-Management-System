# Performance Conventions

> **Rule Hierarchy Level:** 12
> **Applies To:** All
> **Last Updated:** 2026-07-14

## Purpose
This document outlines the performance standards, latency budgets, and resource optimization rules for both backend and frontend development. It enforces efficient data structures, asynchronous programming models, query optimization, caching strategies, and resource management.

## Scope
This convention covers memory allocation, async patterns, caching systems, database query profiling, and lazy loading strategies. It does NOT cover database schema naming (see [Database Convention](file:///d:/My%20Project/rules/05-database/database-convention.md)) or server infrastructure provisioning (see [Deployment Convention](file:///d:/My%20Project/rules/15-deployment/deployment-convention.md)).

## Principles
1. **Async-by-Default:** All I/O operations (network, database, files) MUST run asynchronously to avoid blocking threads.
2. **Resource Stewardship:** Allocate only the memory, connections, and buffers absolutely necessary. Dispose of resources eagerly.
3. **Optimized Queries:** Retrieve only the minimal fields and rows needed from database systems.
4. **Caching Strategy:** Cache computationally expensive or static data at the nearest possible boundary to the user.

---

## Mandatory Rules

### PERF-001: Mandatory Async for I/O
All database, networking, cache, and filesystem operations MUST utilize asynchronous APIs (`async/await`). Blocking synchronous calls (such as `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, or synchronous DB calls like `.ToList()`) are strictly prohibited in application logic.
- **Rationale:** Thread pool starvation occurs when threads are blocked waiting for database or network calls, drastically reducing system throughput.
- **Consequence:** Synchronous blocks cause application freezes under high load as all available execution threads become bottlenecked.

### PERF-002: Propagation of CancellationTokens
All asynchronous methods that accept a `CancellationToken` MUST propagate it to all downstream async calls (such as database queries, HTTP calls, and task delays).
- **Rationale:** Allows the system to cancel resource-heavy database queries and network calls immediately when a client aborts the connection.
- **Consequence:** Long-running operations continue consuming database CPU and memory even after the user has refreshed their page or disconnected.

### PERF-003: No N+1 Queries
Loading related entities in a loop (the N+1 query problem) is strictly prohibited. Developers MUST fetch related entities using eager loading (e.g., SQL `JOIN` equivalents), projections, or batched queries.
- **Rationale:** Executing N additional roundtrips to the database to fetch child records degrades performance exponentially.
- **Consequence:** An endpoint that loads 100 rows will make 101 database queries, turning a 10ms operation into a 2-second bottleneck.

### PERF-004: Explicit Projection in Database Queries
Read-only queries targeting the database MUST project the data into a specific Data Transfer Object (DTO) using selection models. Retrieving full database entities with unused columns and tracking overhead for read-only operations is prohibited.
- **Rationale:** Reduces network payloads, database memory allocation, and avoids overhead associated with tracking entity states in ORM memory.
- **Consequence:** Querying `Select *` on tables with long text columns blocks database buffers and allocates excessive memory on the web server.

### PERF-005: Eager Resource Disposal
All resources implementing cleanup interfaces (such as `IDisposable` or `IAsyncDisposable`, including database connections, HTTP clients, streams, and file handles) MUST be managed using `using` statements, `using` declarations, or explicit try-finally cleanup blocks.
- **Rationale:** Prevents memory leaks and file/connection pool exhaustion.
- **Consequence:** Leaked database connections exhaust the connection pool, locking out new requests and crashing the app.

---

## Recommended Practices

### PERF-050: Cache-Aside Pattern
For read-heavy, low-frequency write resources, applications SHOULD implement the cache-aside pattern (checking cache first, writing database result to cache, invalidating cache on update).
- **Rationale:** Drastically reduces database load and response times.

### PERF-051: Avoid Large Object Allocations
Avoid allocating objects larger than 85,000 bytes (Large Object Heap limit in C#/.NET) inside high-frequency execution paths to prevent garbage collection pauses.
- **Rationale:** Keeps application memory usage stable and prevents micro-stutters during garbage collection runs.

---

## Anti-patterns

### Blocking Thread Execution
```csharp
// ❌ Anti-pattern: Blocking async thread execution
var data = SomeAsyncService.GetDataAsync().Result; 
```
- **Why it's harmful:** Prone to thread-deadlocks and causes thread-pool starvation.
- **What to do instead:** Use `await` keyword.

### Virtual Property Lazy-Loading Abuse
Defining database entities with automatic lazy loading properties that trigger multiple queries during model serialization.
- **Why it's harmful:** Transparently triggers N+1 database queries when serialization libraries traverse the object tree.
- **What to do instead:** Disable automatic lazy loading and explicitly specify related tables to load during query construction.

---

## Examples

### ✅ Correct (Asynchronous, Projection, CancellationToken)

**Repository & Query Code:**
```csharp
public async Task<List<MenuCardDto>> GetActiveMenuItemsAsync(
    int categoryId, 
    CancellationToken cancellationToken)
{
    // Retrieve only necessary fields, no tracking overhead, cancels immediately if aborted
    return await _context.MenuItems
        .AsNoTracking()
        .Where(item => item.CategoryId == categoryId && !item.IsDeleted)
        .Select(item => new MenuCardDto
        {
            Id = item.Id,
            Name = item.Name,
            Price = item.Price,
            ImageUrl = item.ImageUrl,
            IsSoldOut = item.StockQuantity <= 0
        })
        .ToListAsync(cancellationToken);
}
```

### ❌ Incorrect (Synchronous call, N+1 loading, entire Entity retrieved)

```csharp
public List<MenuItem> GetMenuItems(int categoryId)
{
    // ❌ BLOCKING: .Result blocks thread execution
    var category = _context.Categories.FindAsync(categoryId).Result;
    
    // ❌ UNTRACKED LOAD: Retrieves entire database entities with tracking overhead
    var items = _context.MenuItems
        .Where(i => i.CategoryId == categoryId)
        .ToList(); // ❌ Synchronous DB call
        
    // ❌ N+1 QUERY: Executes query inside loop to fetch related reviews
    foreach (var item in items)
    {
        item.Reviews = _context.Reviews.Where(r => r.MenuItemId == item.Id).ToList();
    }
    
    return items;
}
```

---

## Checklist
- [ ] Are all database, file system, and API operations asynchronous?
- [ ] Are `CancellationToken` instances propagated down to all async calls?
- [ ] Do read-only database queries use `.AsNoTracking()` (or equivalent) and project into custom DTOs?
- [ ] Have all loops containing database queries been replaced with Joins or batch queries?
- [ ] Are all classes implementing `IDisposable` or `IAsyncDisposable` enclosed in `using` blocks?
