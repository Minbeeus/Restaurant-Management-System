# SQL Server & Entity Framework Core Conventions

> **Rule Hierarchy Level:** Extension (Framework-Specific)
> **Applies To:** Database / Infrastructure (SQL Server & EF Core 9.0)
> **Last Updated:** 2026-07-14

## Purpose
This document defines the schema configuration, migration safety guidelines, indexing protocols, transaction scopes, and query performance optimizations required when utilizing SQL Server and Entity Framework Core 9.0.

## Scope
This convention covers Fluent API mappings, database entity configurations, migration deployments, connection parameters, and query performance optimization. It does NOT cover application service architecture (see [ASP.NET Core & C# 13 Extension](file:///d:/My%20Project/rules/extensions/aspnet/aspnet-convention.md)).

## Principles
1. **Explicit Fluent Mapping:** Keep Domain entities clean of ORM pollution; configure all tables using isolated Fluent API configuration files.
2. **Migration Quality Assurance:** Never apply migrations without inspecting the raw SQL code generated.
3. **No-Tracking by Default:** Optimize query performance by querying read-only operations with no-tracking flags.
4. **Relational Integrity:** Enforce foreign keys, unique bounds, and validation constraints at the database engine level.

---

## Mandatory Rules

### SQL-001: Explicit Fluent Configuration
All entity mappings, column names, relationships, primary/foreign keys, and database constraints MUST be configured using separate configuration classes implementing `IEntityTypeConfiguration<T>` in the Infrastructure layer. The use of Data Annotations attributes (such as `[Table]`, `[Key]`, `[Required]`, `[Column]`) on Domain entities is strictly prohibited.
- **Rationale:** Keeps Domain models pure and decoupled from ORM-specific details, separating database mapping structures from clean business domain models.
- **Consequence:** Data annotations clutter Domain entities with persistence metadata and violate project boundaries.

### SQL-002: SQL Server Database Naming Conventions
All database schemas MUST adhere to the following naming conventions:
- **Table Names:** PascalCase, pluralized (e.g., `Users`, `Orders`, `MenuItems`).
- **Column Names:** PascalCase (e.g., `CustomerCode`, `LoyaltyPoints`, `CreatedAt`).
- **Primary Keys:** Always named `Id`.
- **Foreign Keys:** Named `[SingularTableName]Id` (e.g., `CategoryId`, `CustomerId`).
- **Indexes:** Prefix with `IX_[TableName]_[ColumnName]`.
- **Unique Constraints:** Prefix with `UQ_[TableName]_[ColumnName]`.
- **Rationale:** Ensures clean mapping, consistent SQL querying, and aligns schema directly to C# properties.
- **Consequence:** Non-matching column names create confusing SQL translations and increase configuration overhead.

### SQL-003: Database Migrations Safety Gate
AI Agents and developers MUST NOT execute `dotnet ef database update` directly on production environments.
- Every entity schema change MUST reside in a single, descriptive migration file.
- The developer or agent MUST run `dotnet ef migrations script` to generate the raw SQL script representing the schema delta.
- The generated SQL MUST be manually reviewed to check for structural anomalies, data loss alerts, or unintended `DROP` commands before application.
- **Rationale:** Protects live databases from database corruption or downtime caused by auto-generated schema drops.
- **Consequence:** EF Core migrations can generate destructive statements (like dropping and recreating a table) to execute a minor column change, deleting production records.

### SQL-004: Mandatory AsNoTracking for Read-Only Queries
All queries fetching data from the database that are not intended for immediate modification in the current transaction block MUST utilize `.AsNoTracking()`.
- **Rationale:** Bypasses state-tracking buffers, saving significant web server memory and CPU cycles during data serialization.
- **Consequence:** Querying list endpoints without `AsNoTracking` keeps thousands of objects tracked in DbContext memory, causing slow garbage collection.

### SQL-005: Raw SQL Parameterization
When raw SQL queries are required (using EF Core `FromSqlInterpolated` or Dapper), SQL strings MUST NOT contain concatenated string inputs. All user parameters MUST be passed using database variables (`DbParameter` or interpolated values parsed by the ORM).
- **Rationale:** Completely immunizes the database against SQL Injection attacks.
- **Consequence:** String concatenation in SQL leads to SQL Injection vulnerabilities, allowing unauthorized data access or database destruction.

### SQL-006: Soft Delete Global Query Filter
All tables storing business-critical transactional records (such as orders, products, customers) MUST implement a soft delete flag (`IsDeleted`). The entity configuration MUST configure a global query filter:
- `.HasQueryFilter(e => !e.IsDeleted)`
- Read queries that intentionally require deleted data must explicitly append `.IgnoreQueryFilters()`.
- **Rationale:** Prevents physical deletion of historical business logs, ensuring historical compliance and accounting audit trails.
- **Consequence:** Physical deletions corrupt order histories and break operational analytics.

---

## Recommended Practices

### SQL-050: DbContext Interceptor for Audit Fields
Save operations SHOULD use a EF Core `SaveChangesInterceptor` to automatically populate audit fields (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) on save.
- **Rationale:** Standardizes data audits, ensuring every write contains author logs without manual code injection.

---

## Anti-patterns

### Synchronous SaveChanges
Invoking `_context.SaveChanges()` instead of using `await _context.SaveChangesAsync(cancellationToken)`.
- **Why it's harmful:** Blocks web server worker threads during network transit to the SQL server, reducing concurrency.
- **What to do instead:** Always use the async version passing the active cancellation token.

### Eager-Loading in Loops
Running `Include` statements recursively on giant nested trees without bounds.
- **Why it's harmful:** Triggers Cartesian explosion in SQL Server, loading redundant rows and exhausting database memory.
- **What to do instead:** Use explicit selection projections or split query options (`AsSplitQuery()`).

---

## Examples

### ✅ Correct (Fluent Configuration, SaveChanges Interceptor, Safe Query)

**Entity Configuration (`src/Restaurant.Infrastructure/Persistence/Configurations/OrderConfiguration.cs`):**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Fluent Configuration (Zero annotations on domain models)
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        // Global query filter for soft delete
        builder.HasQueryFilter(o => !o.IsDeleted);

        // Index for performance
        builder.HasIndex(o => o.OrderCode)
            .HasDatabaseName("IX_Orders_OrderCode")
            .IsUnique();

        // Foreign Key Setup
        builder.HasOne(o => o.Customer)
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**EF Core Audit Interceptor (`src/Restaurant.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs`):**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Restaurant.Domain.Entities;

namespace Restaurant.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = "system"; // Retrieve from user service
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = "system";
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### ❌ Incorrect (Data annotations in Domain, hardcoded query, physical delete)

**Domain Model polluted with database rules:**
```csharp
// ❌ Violates Domain purity (Domain layer depends on System.ComponentModel.DataAnnotations)
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant.Domain.Entities
{
    [Table("tbl_Order")] // ❌ Violates SQL-002 naming
    public class Order
    {
        [Key]
        public int OrderId { get; set; } // ❌ Violates SQL-002: PK must be named 'Id'
        
        [Required]
        [Column("txt_code")]
        public string OrderCode { get; set; }
    }
}
```

---

## Checklist
- [ ] Are all database entity configurations written via fluent `IEntityTypeConfiguration<T>` classes?
- [ ] Do table and column names conform strictly to PascalCase and primary keys named `Id`?
- [ ] Have you generated a migration and reviewed the raw SQL script before applying changes?
- [ ] Are all read-only queries utilizing `.AsNoTracking()`?
- [ ] Are raw SQL parameters parsed through interpolation parameters rather than string concatenation?
- [ ] Is soft delete configured as a global query filter on all transactional database tables?
- [ ] Are audit fields automatically populated via EF Core SaveChanges Interceptors?
- [ ] Are database operations asynchronous, using cancellation tokens?
