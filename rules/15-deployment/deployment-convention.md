# Deployment Conventions

> **Rule Hierarchy Level:** 15
> **Applies To:** All
> **Last Updated:** 2026-07-14

## Purpose
This document defines the deployment standards, pipeline validation, environment configuration, rollback safety procedures, health checks, and monitoring required for reliable and reproducible production releases.

## Scope
This convention covers CI/CD pipeline stages, environment configurations, deployment rollbacks, health probe configurations, and observability. It does NOT cover development workflows (see [Git Convention](file:///d:/My%20Project/rules/13-git/git-convention.md)) or application performance targets (see [Performance Convention](file:///d:/My%20Project/rules/12-performance/performance-convention.md)).

## Principles
1. **Immutable Configuration:** Build artifacts once, and deploy them across varying environments using external configuration variables.
2. **Fail-Safe Pipelines:** Automation MUST block deployment if tests fail, vulnerabilities are detected, or validation checks fail.
3. **Painless Rollback:** Every production release MUST have an automated, verified path to revert to the previous stable state.
4. **Observable Deployments:** Applications must expose active health indicators and logs to monitor success during rollout.

---

## Mandatory Rules

### DEPLOY-001: CI Build and Test Gate
The CI/CD pipeline MUST block the deployment of any build if unit or integration tests fail, or if compiling errors are encountered. Bypassing test verification gates for release builds is strictly prohibited.
- **Rationale:** Guarantees that only structurally sound, verified builds enter testing and production environments.
- **Consequence:** Bypassing test gates leads to immediate runtime crashes, regressions, and outages.

### DEPLOY-002: Environment Configuration Isolation
Application configurations MUST be isolated by environment. Hardcoding connection strings, endpoints, security configurations, or secrets directly in compiled code is prohibited. Configuration values MUST be loaded via environment variables, container secrets, or specialized secure vaults.
- **Rationale:** Protects environment integrity, ensures security credentials remain isolated, and prevents production data contamination.
- **Consequence:** Storing development credentials in production configuration structures can cause developer actions to overwrite live production databases.

### DEPLOY-003: Database Migration Rollback Scripts
Every database migration script deployed via a release pipeline MUST include a matching, automated rollback script (`Down()` method equivalent). The rollback path MUST be verified on a test database environment before launching the deployment.
- **Rationale:** Restores system operation immediately if a deployment fails due to database errors or schema mismatches.
- **Consequence:** Unverified database migrations that fail during deployment lock tables, leaving the system in a half-migrated, corrupted state with no clear recovery path.

### DEPLOY-004: Standardized Health Check Probes
All web applications and microservices MUST expose standardized HTTP health probes:
- `/health/live` (Liveness): Returns `200 OK` when the process is running. Used to determine if the container should be restarted.
- `/health/ready` (Readiness): Returns `200 OK` only when the service can accept traffic (i.e., database connection, caching service, and external APIs are responsive).
- **Rationale:** Allows container orchestrators (like Kubernetes or cloud app platforms) to route traffic safely and perform zero-downtime rolling updates.
- **Consequence:** Without readiness checks, containers receive traffic before they finish initializing, causing 503 errors.

### DEPLOY-005: Automated Rollback on Startup Failure
Deployment orchestrators MUST monitor liveness/readiness probes during rollout. If a new deployment fails health checks repeatedly within a designated window, the orchestrator MUST automatically rollback to the previous stable build.
- **Rationale:** Limits exposure of bad builds to users, keeping system downtime to near zero.
- **Consequence:** Leaving a broken build online forces manual ops intervention, increasing outage times.

---

## Recommended Practices

### DEPLOY-050: Progressive Rollout (Canary / Blue-Green)
For high-availability services, deployments SHOULD use Blue-Green or progressive Canary rollouts to redirect traffic gradually.
- **Rationale:** Mitigates risk by exposing the new build to only a small subset of users before committing to 100% rollout.

### DEPLOY-051: Infrastructure as Code (IaC)
All deployment infrastructure (virtual networks, database instances, storage buckets, app services) SHOULD be defined and versioned using Infrastructure as Code tools (e.g., Terraform, CloudFormation, Bicep).
- **Rationale:** Guarantees that environments can be spun up or recovered repeatably with zero configuration drift.

---

## Anti-patterns

### SSH Deployments
Manually copying files to a server over FTP/SFTP, or running compile commands directly on production servers.
- **Why it's harmful:** Creates untraceable, non-reproducible environment configurations and increases human error risk.
- **What to do instead:** All changes MUST be deployed via automated pipelines triggered by git events.

### Incomplete Database Migrations
Deploying new application code that depends on database changes BEFORE verifying that the database migration successfully completed.
- **Why it's harmful:** The new code crashes immediately upon trying to read columns that do not exist yet.
- **What to do instead:** Run migrations as an independent, prerequisite step in the deployment flow before restarting app containers.

---

## Examples

### ✅ Correct (Structured Health Check Endpoint, Appsettings configuration)

**Web Application Health Check Setup:**
```csharp
// Program.cs configuration
var builder = WebApplication.CreateBuilder(args);

// Add health checks for critical dependencies
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
                  ?? throw new InvalidOperationException("Connection string not found"))
    .AddRedis(builder.Configuration["Redis:ConnectionString"] 
              ?? throw new InvalidOperationException("Redis configuration not found"));

var app = builder.Build();

// Expose endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness only checks if the app host is up
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") // Readiness checks all dependencies
});
```

### ❌ Incorrect (Hardcoded connection strings, manual error handling, no health endpoint)

```csharp
// Program.cs
var app = WebApplication.CreateBuilder(args).Build();

// ❌ Hardcoded database connection string in code
string dbConn = "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;";

app.MapGet("/data", () => {
    // Queries database directly using hardcoded credentials
});

// No health probes mapped, orchestrators have no way of knowing if app is functional
app.Run();
```

---

## Checklist
- [ ] Are all compilation, unit test, and lint gates passing green before release?
- [ ] Are all database connection strings, API keys, and environment configs externalized?
- [ ] Do all database migrations have a verified Down() rollback script?
- [ ] Are `/health/live` and `/health/ready` endpoints exposed and validating dependencies?
- [ ] Is the deployment orchestrator configured to automatically rollback if health checks fail during rollout?
