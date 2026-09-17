# Database Convention

> **Rule Hierarchy Level:** 05
> **Applies To:** Database
> **Last Updated:** 2025-07-14

## Purpose

This document defines mandatory rules and recommended practices for all database design, schema management, and data access patterns. It ensures consistency in naming, structural integrity, migration safety, and long-term maintainability of the data layer. All teams working with the database — whether writing migrations, defining entities, or constructing queries — MUST follow these conventions.

## Scope

This convention covers:
- Table, column, key, and constraint naming standards
- Primary key, foreign key, and constraint design
- Migration creation, review, and deployment strategy
- Transaction management and isolation levels
- Index design and maintenance
- Soft delete policy and audit field requirements
- Data integrity and type mapping rules

This convention does NOT cover:
- ORM-specific configuration patterns (see **Infrastructure Convention**)
- API serialization or DTO mapping (see **API Design Convention**)
- Caching strategies or read-replica routing (see **Performance Convention**)
- Connection string management or secrets (see **Security Convention**)

## Principles

1. **Schema Is the Source of Truth** — The database schema enforces business rules through constraints, keys, and types. Application-level validation supplements but never replaces database-level integrity.

2. **Naming Is Communication** — Every table, column, and constraint name MUST clearly convey its purpose without requiring additional documentation. Consistent naming eliminates ambiguity across teams.

3. **Migrations Are Immutable History** — Once a migration has been committed to a shared branch, it is a permanent record. Corrections are made through new compensating migrations, never by altering existing ones.

4. **Data Is Never Truly Deleted** — Business data carries audit, legal, and analytical value. Physical deletion is prohibited; soft delete patterns preserve data while honoring user-facing removal semantics.

5. **Least Privilege, Least Lock** — Transactions MUST be as short and narrow as possible. Acquire locks late, release them early, and always use the minimum isolation level that guarantees correctness.

## Mandatory Rules

### Table & Column Naming

**DB-001: Tables MUST use PascalCase plural names**
Tables represent collections and MUST be named as plural nouns in PascalCase.
- *Rationale:* Plural names reflect that a table holds multiple records. PascalCase aligns with C# entity class mapping conventions.
- *Consequence:* Schema review will reject tables with singular, snake_case, or camelCase names.

**DB-002: Columns MUST use PascalCase**
All column names MUST use PascalCase without underscores or prefixes.
- *Rationale:* Maintains direct 1:1 mapping with C# entity properties, eliminating the need for manual column-name configuration.
- *Consequence:* Columns with non-PascalCase names will be flagged during migration review.

**DB-003: Primary keys MUST be named `Id`**
Every table's primary key column MUST be named `Id`, without a table-name prefix.
- *Rationale:* Provides a universal, predictable identifier column across all entities. Simplifies generic repository patterns and base entity classes.
- *Consequence:* Entities with non-standard PK names (e.g., `OrderId` as PK) will fail code review.

**DB-004: Foreign key columns MUST follow `[SingularTableName]Id` format**
A foreign key referencing the `Orders` table MUST be named `OrderId`, not `Order_Id`, `FK_Order`, or `OrdId`.
- *Rationale:* Creates an immediately recognizable relationship pattern. Enables convention-based ORM configuration.
- *Consequence:* Non-conforming FK column names will be rejected in schema review.

**DB-005: Junction tables MUST combine both related table names**
Many-to-many join tables MUST be named by combining both table names in alphabetical or logical order using PascalCase.
- *Rationale:* Clearly communicates the relationship. Enables teams to locate join tables without consulting documentation.
- *Consequence:* Ambiguously named junction tables will be renamed via corrective migration.

### Keys & Constraints

**DB-006: Every table MUST have a primary key**
No table may exist without a defined primary key, including staging, logging, or temporary tables.
- *Rationale:* Primary keys are essential for row identity, ORM mapping, replication, and change tracking.
- *Consequence:* Tables without primary keys will be blocked from deployment.

**DB-007: Foreign key constraints MUST be explicitly defined**
All relationships MUST have formal FK constraints at the database level, not just application-level navigation properties.
- *Rationale:* Database-level FK constraints guarantee referential integrity regardless of which application or script modifies the data.
- *Consequence:* Missing FK constraints will be caught in migration review and must be added before merge.

**DB-008: Unique constraints MUST be defined for natural keys**
If a business rule dictates uniqueness (e.g., employee code, email, SKU), a unique constraint or unique index MUST be created.
- *Rationale:* Application-level checks are subject to race conditions. Database constraints are the only reliable uniqueness guarantee.
- *Consequence:* Duplicate data incidents caused by missing constraints are treated as critical bugs.

**DB-009: Check constraints MUST enforce domain validation**
Columns with known value ranges or domain rules MUST use CHECK constraints (e.g., `Quantity > 0`, `Discount BETWEEN 0 AND 100`).
- *Rationale:* Prevents invalid data from entering the database regardless of the entry point — API, migration script, or manual fix.
- *Consequence:* Columns lacking appropriate CHECK constraints will be flagged during review.

### Migration Strategy

**DB-010: Each migration MUST represent one logical change**
A single migration MUST NOT combine unrelated schema changes (e.g., adding a new table AND renaming a column on an unrelated table).
- *Rationale:* Atomic migrations simplify rollback, debugging, and deployment sequencing.
- *Consequence:* Multi-concern migrations will be rejected and must be split.

**DB-011: Migration names MUST be descriptive**
Migration names MUST clearly describe the change (e.g., `AddLoyaltyPointsToCustomers`, `CreateOrderItemsTable`).
- *Rationale:* Migration history serves as a change log. Descriptive names enable fast navigation without reading the migration body.
- *Consequence:* Migrations with auto-generated or vague names (e.g., `Migration_20250714`) will be rejected.

**DB-012: Generated SQL MUST be reviewed before applying**
The developer MUST inspect the generated SQL script of every migration before executing it against any environment.
- *Rationale:* ORM-generated SQL may contain unexpected DROP statements, implicit data loss, or inefficient operations.
- *Consequence:* Unapproved migrations applied to shared environments will trigger an incident review.

**DB-013: Existing migrations MUST NOT be modified**
Once a migration is committed to a shared branch, its content MUST NOT change. Corrections require a new compensating migration.
- *Rationale:* Modifying applied migrations causes checksum mismatches, broken environments, and deployment failures.
- *Consequence:* Modified migrations will break CI pipelines and block deployments.

**DB-014: Database update commands MUST NOT auto-run**
Build scripts, CI/CD pipelines, and application startup MUST NOT execute `database update` or equivalent commands automatically. Migrations MUST be applied through an explicit, approved deployment step.
- *Rationale:* Auto-migration can cause data loss in production, apply untested changes, or run against the wrong environment.
- *Consequence:* Any pipeline or startup code that auto-applies migrations will be immediately disabled.

**DB-015: Every migration MUST have a rollback strategy**
Each migration MUST have a documented or implemented `Down()` method or a corresponding rollback script.
- *Rationale:* Failed deployments require fast rollback. Missing rollback paths turn minor issues into prolonged outages.
- *Consequence:* Migrations without rollback capability will be blocked from production deployment.

### Transaction Management

**DB-016: Multi-step data operations MUST use explicit transactions**
Any operation that modifies more than one table, or performs multiple related writes, MUST wrap those operations in an explicit transaction.
- *Rationale:* Implicit transactions per-statement leave the database in an inconsistent state if an intermediate step fails.
- *Consequence:* Multi-step operations without explicit transactions are treated as critical bugs.

**DB-017: Transaction scope MUST be minimized**
Transactions MUST encompass only the statements that require atomicity. Long-running work (HTTP calls, file I/O, complex computation) MUST NOT occur inside a transaction.
- *Rationale:* Long transactions hold locks, degrade concurrency, and increase deadlock risk.
- *Consequence:* Transactions containing external calls will be refactored during review.

### Soft Delete

**DB-018: Business data MUST NOT be physically deleted**
DELETE statements against business entity tables are prohibited. All removals MUST use soft delete.
- *Rationale:* Physical deletion destroys audit trails, breaks referential integrity, and violates data retention policies.
- *Consequence:* Any hard-delete operation on business data will be treated as a critical incident.

**DB-019: Soft-deleted records MUST use `IsDeleted`, `DeletedAt`, `DeletedBy` columns**
Every soft-deletable entity MUST include all three columns: a boolean flag, a timestamp, and an actor identifier.
- *Rationale:* `IsDeleted` enables efficient filtering. `DeletedAt` and `DeletedBy` provide full audit trail for the deletion event.
- *Consequence:* Entities with incomplete soft delete columns will fail schema review.

**DB-020: All queries MUST filter out soft-deleted records by default**
Global query filters or equivalent mechanisms MUST exclude records where `IsDeleted = true` from all standard queries.
- *Rationale:* Prevents accidentally displaying or processing deleted records. Opt-in inclusion is safer than opt-out exclusion.
- *Consequence:* Queries returning soft-deleted records unintentionally are treated as data-leak bugs.

### Audit Fields

**DB-021: All business tables MUST include audit columns**
Every business entity table MUST have `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy` columns.
- *Rationale:* Audit fields are essential for debugging, compliance, incident investigation, and user accountability.
- *Consequence:* Tables without audit columns will be blocked from deployment.

**DB-022: Audit fields MUST be populated automatically**
Audit columns MUST be set through EF Core interceptors, database triggers, or equivalent middleware — never manually in business logic code.
- *Rationale:* Manual population is error-prone and inconsistent. Automated mechanisms guarantee every write is tracked.
- *Consequence:* Business logic that manually sets audit fields will be refactored to use the centralized mechanism.

### Data Integrity

**DB-023: Referential integrity MUST be enforced at the database level**
All entity relationships MUST have corresponding FK constraints. Application-level navigation properties alone are insufficient.
- *Rationale:* External scripts, data fixes, and alternative access paths bypass application logic. Only database constraints are universally enforced.
- *Consequence:* Orphaned records caused by missing FK constraints are treated as data integrity incidents.

**DB-024: Enum values MUST be stored as integers in the database**
Enum columns MUST use `int` (or `smallint`/`tinyint`) storage. String-based enum storage is prohibited.
- *Rationale:* Integer storage is compact, indexable, and performant. String serialization is handled at the API layer (see **API Design Convention**).
- *Consequence:* Enum columns stored as strings will require corrective migration.

**DB-025: Column types and sizes MUST match the domain**
Every column MUST use the most appropriate type and size for its data. Avoid `nvarchar(max)` when a bounded length is known. Avoid `float` for monetary values.
- *Rationale:* Appropriate types prevent data corruption, reduce storage, and improve query performance.
- *Consequence:* Over-sized or incorrect column types will be flagged during schema review.

## Recommended Practices

**DB-050: Indexes SHOULD be created on frequently queried columns**
Columns that appear in WHERE, JOIN, or ORDER BY clauses of common queries SHOULD have supporting indexes.
- *Rationale:* Missing indexes cause full table scans, degrading performance as data grows.
- *Consequence:* Slow queries traced to missing indexes will trigger an indexing review.

**DB-051: Foreign key columns SHOULD be indexed**
Every FK column SHOULD have a corresponding index unless the table is very small and unlikely to grow.
- *Rationale:* FK columns are frequently used in JOINs. Without an index, join performance degrades significantly.
- *Consequence:* Performance degradation on join-heavy queries.

**DB-052: Composite indexes SHOULD be created for common multi-column query patterns**
When queries routinely filter on the same combination of columns, a composite index SHOULD be created with columns ordered by selectivity (most selective first).
- *Rationale:* Composite indexes eliminate the need for multiple single-column index lookups.
- *Consequence:* Suboptimal query plans for multi-column filters.

**DB-053: Index inventory SHOULD be reviewed quarterly**
Unused, duplicate, or overlapping indexes SHOULD be identified and removed on a regular cadence.
- *Rationale:* Over-indexing degrades write performance and wastes storage. Index maintenance cost grows with table size.
- *Consequence:* Accumulated unused indexes slow down inserts and updates.

**DB-054: Transactions SHOULD use the lowest sufficient isolation level**
Default to `READ COMMITTED`. Use `SERIALIZABLE` or `REPEATABLE READ` only when the business operation specifically requires it, with documented justification.
- *Rationale:* Higher isolation levels increase lock contention and deadlock probability.
- *Consequence:* Unnecessary serializable transactions reduce throughput.

**DB-055: Default values SHOULD be defined for non-nullable columns where a sensible default exists**
Columns like `IsDeleted` (default `false`), `CreatedAt` (default `GETUTCDATE()`), and `Quantity` (default `0`) SHOULD have database-level defaults.
- *Rationale:* Reduces application-level boilerplate and prevents null-related insertion failures.
- *Consequence:* Missing defaults cause unnecessary insert errors.

**DB-056: Archival strategy SHOULD exist for aged soft-deleted records**
Soft-deleted records older than a defined retention period SHOULD be moved to archive tables or a separate archive database.
- *Rationale:* Accumulated soft-deleted records bloat tables, degrade query performance, and increase backup sizes.
- *Consequence:* Growing table sizes with diminishing query performance over time.

**DB-057: Operations SHOULD be idempotent where possible**
Data operations, especially those triggered by message queues or retry logic, SHOULD produce the same result when executed multiple times.
- *Rationale:* Network failures and retries are inevitable. Idempotent operations prevent duplicate data.
- *Consequence:* Non-idempotent operations cause duplicate records on retry.

## Anti-patterns

### 🚫 Magic Column Names
- **Description:** Using abbreviations, acronyms, or cryptic column names like `CstCd`, `Qty`, `Amt`, or `Flg`.
- **Why it's harmful:** Forces every developer to learn a non-standard vocabulary. Causes bugs from misinterpreting column purpose.
- **What to do instead:** Use full PascalCase names: `CustomerCode`, `Quantity`, `Amount`, `IsActive`.

### 🚫 Implicit Relationships
- **Description:** Relying on matching column values between tables without defining formal FK constraints.
- **Why it's harmful:** Nothing prevents orphaned records. Data integrity depends entirely on application code being correct.
- **What to do instead:** Always define FK constraints at the database level (DB-007).

### 🚫 God Migration
- **Description:** Combining multiple unrelated changes (new tables, column renames, data seeding) in a single migration.
- **Why it's harmful:** Impossible to partially rollback. Difficult to review. Hides breaking changes.
- **What to do instead:** One migration per logical change (DB-010).

### 🚫 Hard Delete Business Data
- **Description:** Using `DELETE FROM Orders WHERE Id = @id` on business entities.
- **Why it's harmful:** Permanently destroys records, breaks FK chains, violates audit and compliance requirements.
- **What to do instead:** Set `IsDeleted = true`, `DeletedAt = GETUTCDATE()`, `DeletedBy = @currentUser` (DB-018, DB-019).

### 🚫 Catch-All nvarchar(max)
- **Description:** Defaulting every string column to `nvarchar(max)` instead of specifying appropriate lengths.
- **Why it's harmful:** Prevents indexing (columns over 900 bytes cannot be indexed), wastes storage, hides design decisions.
- **What to do instead:** Define explicit lengths based on domain analysis: `Name nvarchar(200)`, `Code nvarchar(50)`.

### 🚫 Transaction-Wrapped External Calls
- **Description:** Making HTTP requests, sending emails, or performing file I/O inside a database transaction.
- **Why it's harmful:** Holds database locks for the duration of the external call. Network latency or timeout causes prolonged locks and deadlocks.
- **What to do instead:** Perform external calls outside the transaction. Use the outbox pattern for guaranteed delivery.

## Examples

### ✅ Correct Table and Column Naming
```sql
CREATE TABLE MenuItems (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(200)   NOT NULL,
    Description     NVARCHAR(1000)  NULL,
    Price           DECIMAL(18,2)   NOT NULL,
    CategoryId      INT             NOT NULL,
    IsAvailable     BIT             NOT NULL DEFAULT 1,
    IsDeleted       BIT             NOT NULL DEFAULT 0,
    DeletedAt       DATETIME2       NULL,
    DeletedBy       NVARCHAR(100)   NULL,
    CreatedAt       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    CreatedBy       NVARCHAR(100)   NOT NULL,
    UpdatedAt       DATETIME2       NULL,
    UpdatedBy       NVARCHAR(100)   NULL,

    CONSTRAINT FK_MenuItems_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(Id),
    CONSTRAINT CK_MenuItems_Price CHECK (Price >= 0)
);

CREATE INDEX IX_MenuItems_CategoryId ON MenuItems(CategoryId);
CREATE INDEX IX_MenuItems_IsDeleted ON MenuItems(IsDeleted) WHERE IsDeleted = 0;
```

### ❌ Incorrect Table and Column Naming
```sql
-- WRONG: singular name, snake_case, abbreviated columns, no constraints
CREATE TABLE menu_item (
    menu_item_id    INT IDENTITY(1,1) PRIMARY KEY,  -- Should be 'Id'
    nm              NVARCHAR(MAX),                   -- Cryptic, oversized
    desc            NVARCHAR(MAX),                   -- Reserved word, oversized
    prc             FLOAT,                           -- Wrong type for money
    cat_id          INT                              -- No FK constraint defined
);
```

### ✅ Correct Junction Table
```sql
CREATE TABLE OrderMenuItems (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    OrderId         INT             NOT NULL,
    MenuItemId      INT             NOT NULL,
    Quantity        INT             NOT NULL DEFAULT 1,
    UnitPrice       DECIMAL(18,2)   NOT NULL,

    CONSTRAINT FK_OrderMenuItems_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(Id),
    CONSTRAINT FK_OrderMenuItems_MenuItems FOREIGN KEY (MenuItemId)
        REFERENCES MenuItems(Id),
    CONSTRAINT CK_OrderMenuItems_Quantity CHECK (Quantity > 0)
);

CREATE INDEX IX_OrderMenuItems_OrderId ON OrderMenuItems(OrderId);
CREATE INDEX IX_OrderMenuItems_MenuItemId ON OrderMenuItems(MenuItemId);
```

### ✅ Correct Migration Naming and Structure (EF Core)
```csharp
// Migration name: AddLoyaltyPointsToCustomers
public partial class AddLoyaltyPointsToCustomers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LoyaltyPoints",
            table: "Customers",
            type: "int",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "LoyaltyPoints",
            table: "Customers");
    }
}
```

### ❌ Incorrect Migration Practice
```csharp
// WRONG: Vague name, no Down(), multiple unrelated changes
public partial class UpdateDatabase : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Unrelated change 1: add column to Customers
        migrationBuilder.AddColumn<int>("LoyaltyPoints", "Customers", ...);

        // Unrelated change 2: rename column on a different table
        migrationBuilder.RenameColumn("Desc", "MenuItems", "Description");

        // Unrelated change 3: create entirely new table
        migrationBuilder.CreateTable("AuditLogs", ...);
    }

    // WRONG: No Down() method — no rollback possible
}
```

### ✅ Correct Soft Delete Query Filter (EF Core)
```csharp
public class ApplicationDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Global filter ensures soft-deleted records are excluded by default
        modelBuilder.Entity<MenuItem>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Order>()
            .HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<Customer>()
            .HasQueryFilter(e => !e.IsDeleted);
    }
}

// Querying — soft-deleted records are automatically excluded
var activeItems = await _context.MenuItems
    .Where(m => m.CategoryId == categoryId)
    .ToListAsync();

// Explicitly include soft-deleted records when needed (admin/audit)
var allItems = await _context.MenuItems
    .IgnoreQueryFilters()
    .Where(m => m.CategoryId == categoryId)
    .ToListAsync();
```

### ✅ Correct Audit Field Auto-Population (EF Core Interceptor)
```csharp
public class AuditFieldInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditFieldInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return ValueTask.FromResult(result);

        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return ValueTask.FromResult(result);
    }
}
```

### ✅ Correct Transaction Usage
```csharp
public async Task PlaceOrderAsync(CreateOrderCommand command)
{
    // Begin explicit transaction for multi-table operation
    await using var transaction = await _context.Database
        .BeginTransactionAsync(IsolationLevel.ReadCommitted);

    try
    {
        var order = new Order { CustomerId = command.CustomerId, Status = OrderStatus.Pending };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var item in command.Items)
        {
            var orderItem = new OrderMenuItem
            {
                OrderId = order.Id,
                MenuItemId = item.MenuItemId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            _context.OrderMenuItems.Add(orderItem);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### ✅ Correct Enum Storage
```csharp
// Domain enum
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Ready = 3,
    Delivered = 4,
    Cancelled = 5
}

// EF Core configuration — stored as int in database
modelBuilder.Entity<Order>()
    .Property(o => o.Status)
    .HasConversion<int>()
    .HasDefaultValue(OrderStatus.Pending);
```

```sql
-- Resulting column in database
ALTER TABLE Orders ADD Status INT NOT NULL DEFAULT 0;
ALTER TABLE Orders ADD CONSTRAINT CK_Orders_Status CHECK (Status BETWEEN 0 AND 5);
```

## Checklist

- □ All table names are PascalCase and plural
- □ All column names are PascalCase without prefixes or abbreviations
- □ Every primary key column is named `Id`
- □ Every foreign key column follows `[SingularTableName]Id` format
- □ Every table has a primary key defined
- □ All relationships have FK constraints at the database level
- □ Natural keys have unique constraints
- □ Domain rules are enforced with CHECK constraints
- □ Each migration contains exactly one logical change
- □ Migration names clearly describe the change
- □ Generated SQL has been reviewed before applying
- □ No existing migrations have been modified
- □ Every migration has a working `Down()` / rollback strategy
- □ No auto-migration on application startup or in CI/CD
- □ Multi-step writes use explicit transactions
- □ No external calls (HTTP, file I/O) inside transactions
- □ Business tables use soft delete (`IsDeleted`, `DeletedAt`, `DeletedBy`)
- □ Global query filters exclude soft-deleted records by default
- □ All business tables have `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- □ Audit fields are populated automatically (interceptor/trigger), not manually
- □ Foreign key columns have supporting indexes
- □ Enum values are stored as integers, not strings
- □ Column types and sizes match the domain (no unnecessary `nvarchar(max)` or `float` for money)
- □ Monetary values use `DECIMAL(18,2)`, not `FLOAT` or `MONEY`
- □ Default values are defined for non-nullable columns where sensible
