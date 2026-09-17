# Logging Convention

> **Rule Hierarchy Level:** 11
> **Applies To:** Backend
> **Last Updated:** 2025-07-14

## Purpose

This document defines the standards for application logging, monitoring, and alerting across all backend services. Consistent logging practices enable effective debugging, incident response, and operational visibility. These conventions ensure that logs are structured, actionable, and compliant with data protection requirements.

## Scope

This convention covers log levels, structured logging formats, sensitive data handling in logs, monitoring and alerting integration, and log retention policies. It does **NOT** cover application error handling logic (see **Error Handling Convention**), database auditing (see **Database Convention**), or frontend console logging (see **Frontend Convention**).

## Principles

1. **Observability First** — Logging exists to provide operational insight. Every log entry MUST contribute to understanding system behavior, diagnosing issues, or auditing actions. Noise reduces the value of the entire logging pipeline.

2. **Structure Over Prose** — Structured logs (key-value pairs) are machine-parseable, searchable, and aggregatable. Free-form text messages defeat the purpose of centralized log analysis.

3. **Security by Default** — Sensitive data MUST never appear in logs. The default posture is to exclude rather than include personal or secret information.

4. **Correlation Across Boundaries** — Every request MUST carry a correlation ID that flows through all services, enabling end-to-end tracing of distributed operations.

5. **Actionability** — Logs at WARN level and above MUST provide enough context for an operator to understand what happened and what to do next without reading source code.

## Mandatory Rules

### Log Levels

**LOG-001: MUST use the correct log level for each message**
- **Rule:** Log messages MUST use the appropriate severity level as defined below:
  - `DEBUG` — Detailed diagnostic information useful only during development or deep troubleshooting. MUST NOT be enabled in production by default.
  - `INFO` — General operational events confirming the system is working as expected (e.g., service startup, request processed successfully, scheduled job completed).
  - `WARN` — Potentially harmful situations that do not prevent operation but indicate something unexpected (e.g., deprecated API usage, retry attempts, configuration fallbacks).
  - `ERROR` — Error events where the application can continue operating but a specific operation failed (e.g., failed payment processing, external API timeout, validation failure on critical path).
  - `FATAL / CRITICAL` — System-level failures requiring immediate human intervention (e.g., database connection pool exhausted, out-of-memory condition, unrecoverable startup failure).
- **Rationale:** Correct leveling enables filtering, alerting thresholds, and efficient log analysis. Misleveled logs either cause alert fatigue or hide critical issues.
- **Consequence:** Misleveled logs will be flagged in code review. Repeated violations require refactoring before merge.

**LOG-002: MUST NOT use Console.WriteLine, Debug.Print, or equivalent for logging**
- **Rule:** All logging MUST go through the framework-provided logging abstraction (e.g., `ILogger<T>` in .NET). Direct console output MUST NOT be used in application code.
- **Rationale:** Logging abstractions provide level filtering, structured output, sink routing, and testability. Console writes bypass all of these capabilities.
- **Consequence:** Code using direct console output for logging will be rejected in code review.

**LOG-003: MUST use structured logging with message templates**
- **Rule:** Log messages MUST use semantic message templates with named placeholders, not string interpolation or concatenation.
- **Rationale:** Structured templates allow log aggregation systems to parse, index, and query individual fields. String interpolation produces unique strings that cannot be grouped or analyzed.
- **Consequence:** Logs using string interpolation will be flagged and must be refactored.

### Sensitive Data Protection

**LOG-004: MUST NOT log passwords, tokens, secrets, or payment data**
- **Rule:** The following data categories MUST NEVER appear in any log at any level: passwords, API keys, access/refresh tokens, credit card numbers, CVVs, bank account numbers, or any authentication secret.
- **Rationale:** Logged secrets can be exfiltrated from log storage, violating security policies and potentially compliance regulations (PCI-DSS, SOC 2).
- **Consequence:** Any occurrence of secrets in logs is treated as a security incident requiring immediate remediation and secret rotation.

**LOG-005: MUST mask or exclude PII before logging**
- **Rule:** Personally Identifiable Information (email, phone, national ID, address) MUST be masked or excluded from log output. If PII is required for debugging, it MUST be masked (e.g., `j***@example.com`, `***-***-1234`).
- **Rationale:** GDPR, CCPA, and similar regulations restrict PII processing. Logs are often stored in systems with broader access than production databases.
- **Consequence:** PII in logs triggers a compliance review and mandatory remediation.

**LOG-006: MUST define and enforce a masking policy per data category**
- **Rule:** Each project MUST maintain a documented masking policy that specifies how each sensitive data category is handled in logs (excluded, masked, hashed, or allowed).
- **Rationale:** Consistent masking prevents accidental exposure when different developers handle the same data types differently.
- **Consequence:** Projects without a documented masking policy will fail compliance audits.

### Correlation and Context

**LOG-007: MUST include a correlation ID in every log entry for request-scoped operations**
- **Rule:** All log entries produced during the processing of an incoming request MUST include a `CorrelationId` property. This ID MUST be propagated to all downstream service calls, background jobs, and event publications triggered by that request.
- **Rationale:** Correlation IDs enable end-to-end tracing across distributed systems and are essential for incident investigation.
- **Consequence:** Logs without correlation IDs are effectively orphaned and cannot be traced to their originating request.

**LOG-008: MUST include contextual data in structured log properties**
- **Rule:** Log entries MUST include relevant contextual properties such as `UserId`, `RequestPath`, `HttpMethod`, `StatusCode`, `ElapsedMs`, and `OperationName` where applicable. These MUST be added via logging scopes or enrichment, not embedded in the message template.
- **Rationale:** Contextual properties enable filtering and correlation without parsing message text.
- **Consequence:** Logs missing key context require additional investigation time during incidents.

## Recommended Practices

**LOG-050: SHOULD use logging scopes for operation-level context**
- **Guideline:** When a logical operation spans multiple log statements, use a logging scope to automatically attach shared properties (e.g., `OrderId`, `CustomerId`) to all entries within that scope.
- **Rationale:** Scopes reduce repetition and ensure consistency across related log entries.

**LOG-051: SHOULD log operation duration for key business operations**
- **Guideline:** Measure and log the elapsed time for significant operations (database queries, external API calls, business process steps) at INFO level.
- **Rationale:** Duration logging provides performance baselines and highlights degradation trends.

**LOG-052: SHOULD configure log level dynamically without redeployment**
- **Guideline:** The logging infrastructure SHOULD support runtime log level changes (e.g., via configuration reload, feature flags, or management endpoints) to enable temporary DEBUG logging in production.
- **Rationale:** Redeploying to change log levels is slow and disruptive during active incidents.

**LOG-053: SHOULD NOT log successful validation or routine operations at WARN or above**
- **Guideline:** Normal-path events such as successful input validation, cache hits, or heartbeat checks SHOULD be logged at DEBUG or INFO, not WARN.
- **Rationale:** Over-use of WARN and ERROR dilutes their signal and causes alert fatigue.

## Monitoring and Alerting

**LOG-060: MUST define alerting thresholds for ERROR and FATAL log rates**
- **Rule:** Each service MUST define thresholds for ERROR and FATAL log rates that trigger alerts. At minimum: FATAL → immediate page; ERROR rate exceeding baseline → alert within 5 minutes.
- **Rationale:** Unmonitored error rates allow failures to go unnoticed until users report them.
- **Consequence:** Services without alerting thresholds will not be approved for production deployment.

**LOG-061: MUST expose health check endpoints**
- **Rule:** Every service MUST expose a health check endpoint (e.g., `/health`) that reports the service's ability to process requests, including connectivity to critical dependencies (database, cache, message broker).
- **Rationale:** Health checks enable load balancers, orchestrators, and monitoring systems to detect and react to unhealthy instances.
- **Consequence:** Services without health checks cannot be deployed to managed environments.

**LOG-062: SHOULD collect metrics for key operations**
- **Guideline:** Services SHOULD emit metrics (counters, histograms, gauges) for key operations: request rate, error rate, latency percentiles, queue depth, cache hit ratio.
- **Rationale:** Metrics provide aggregate views that logs alone cannot efficiently deliver, enabling dashboards and trend analysis.

## Log Retention

**LOG-070: MUST define retention periods per log category**
- **Rule:** Each project MUST define and document log retention periods. Minimum guidelines: ERROR/FATAL logs — 90 days; INFO logs — 30 days; DEBUG logs — 7 days (if collected at all).
- **Rationale:** Unbounded log retention increases storage costs and may violate data minimization principles under GDPR and similar regulations.
- **Consequence:** Projects without defined retention periods will fail compliance audits.

**LOG-071: MUST implement log rotation to prevent disk exhaustion**
- **Rule:** File-based log sinks MUST implement rotation by size or time. Maximum file size MUST NOT exceed 100 MB before rotation. Old files MUST be compressed or deleted per retention policy.
- **Rationale:** Unrotated logs can fill disks and cause application outages.
- **Consequence:** Disk exhaustion caused by unrotated logs is treated as a preventable incident.

**LOG-072: SHOULD consider compliance requirements for log storage location**
- **Guideline:** Logs containing PII (even masked) SHOULD be stored in regions and systems that comply with applicable data residency requirements. Log access SHOULD be audited.
- **Rationale:** Regulatory frameworks may restrict where data is stored and who can access it.

## Anti-patterns

### The Novel Writer
- **Description:** Writing long, human-readable prose messages instead of structured templates with properties.
- **Why it's harmful:** Prose messages cannot be grouped, indexed, or queried by log analysis tools. Each unique string is treated as a distinct event type.
- **What to do instead:** Use message templates with named placeholders. Attach variable data as structured properties.

### The Secret Logger
- **Description:** Logging entire request/response payloads, DTOs, or entities without filtering sensitive fields.
- **Why it's harmful:** Full object serialization captures passwords, tokens, and PII that happen to be in the object graph.
- **What to do instead:** Log only relevant, non-sensitive fields. Use a masking serializer or create dedicated log DTOs.

### The Cry Wolf
- **Description:** Using ERROR or WARN for expected, non-exceptional conditions (e.g., user input validation failure).
- **Why it's harmful:** Alert fatigue. Operators stop investigating WARN/ERROR alerts when they are routinely false positives.
- **What to do instead:** Use INFO or DEBUG for expected conditions. Reserve WARN for genuinely unexpected situations.

### The Silent Catch
- **Description:** Catching exceptions and logging nothing, or logging only "An error occurred" without context or the exception itself.
- **Why it's harmful:** Critical diagnostic information is lost, making incident investigation impossible.
- **What to do instead:** Log the exception object and all relevant context (operation name, input parameters, correlation ID).

## Examples

### ✅ Correct — Structured Logging with Context
```csharp
// Good: message template + structured properties + exception
_logger.LogError(
    ex,
    "Payment processing failed for order {OrderId}, amount {Amount}, gateway {Gateway}",
    order.Id,
    order.TotalAmount,
    gatewayName);
```

### ❌ Incorrect — String Interpolation and Console Output
```csharp
// Bad: string interpolation destroys structure
_logger.LogError($"Payment failed for order {order.Id} amount {order.TotalAmount}");

// Bad: console write bypasses logging pipeline
Console.WriteLine($"Error: {ex.Message}");
```

### ✅ Correct — Sensitive Data Masking
```csharp
// Good: mask email before logging
_logger.LogInformation(
    "User login attempt for {MaskedEmail}",
    MaskingHelper.MaskEmail(user.Email));
```

### ❌ Incorrect — Logging Raw PII
```csharp
// Bad: raw PII in logs
_logger.LogInformation("User login: {Email}, Phone: {Phone}", user.Email, user.Phone);
```

### ✅ Correct — Correlation ID Propagation
```csharp
// Good: correlation ID flows through the pipeline
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = httpContext.TraceIdentifier,
    ["UserId"] = currentUser.Id
}))
{
    _logger.LogInformation("Processing order {OrderId}", orderId);
    await _orderService.ProcessAsync(orderId, cancellationToken);
}
```

## Checklist

- □ All log statements use the logging abstraction (`ILogger`), not `Console.WriteLine`
- □ Log messages use message templates with named placeholders, not string interpolation
- □ Each log statement uses the correct severity level per LOG-001 definitions
- □ No passwords, tokens, API keys, or payment data appear in any log output
- □ PII is masked or excluded from log output per the documented masking policy
- □ Correlation IDs are attached to all request-scoped log entries
- □ Key operations log elapsed duration at INFO level
- □ ERROR and FATAL alerting thresholds are defined and configured
- □ Health check endpoint is implemented and tested
- □ Log retention periods are documented and enforced
- □ Log rotation is configured to prevent disk exhaustion
- □ Exception objects are passed to the logger (not just `ex.Message`)
