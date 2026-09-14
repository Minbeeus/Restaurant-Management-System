# Documentation Conventions

> **Rule Hierarchy Level:** 14
> **Applies To:** All
> **Last Updated:** 2026-07-14

## Purpose
This document establishes the standards for project documentation, covering README files, Architecture Decision Records (ADR), API documentation, technical diagrams, inline code comments, and project changelogs.

## Scope
This convention covers user-facing readmes, developer architecture docs, API contracts, inline comments, and release changelogs. It does NOT cover commit messages (see [Git Convention](file:///d:/My%20Project/rules/13-git/git-convention.md)).

## Principles
1. **Docs as Code:** Documentation MUST reside alongside the codebase in version control, keeping it close to the execution context.
2. **Architecture Traceability:** Major architectural shifts MUST be documented through chronological decision records.
3. **API-First Documentation:** Keep REST and GraphQL API specs synchronized and verified before making structural changes.
4. **Self-Documenting Code:** Write clean, expressive code first; write comments only to clarify *why* something is done, not *what* is done.

---

## Mandatory Rules

### DOC-001: Architecture Decision Records (ADR)
All major architectural changes, dependency additions, framework choices, and design pattern shifts MUST be documented in an Architecture Decision Record (ADR) stored in the repository.
- **Location:** `docs/adr/XXXX-title.md` (where `XXXX` is a sequential 4-digit number).
- **Template:** Every ADR MUST include: Title, Date, Status (Proposed/Accepted/Superseded), Context, Decision, and Consequences.
- **Rationale:** Preserves historical context for future developers regarding why decisions were made, preventing repetitive debates.
- **Consequence:** Without ADRs, architectural decisions are forgotten, leading to structural divergence and accidental reversion of key choices.

### DOC-002: API Contract Synchronization
Any API modification MUST update the corresponding Swagger/OpenAPI or GraphQL schema specification before the change is merged. The API documentation MUST remain accurate, listing parameters, error codes, and response structures.
- **Rationale:** Ensures that frontend, mobile, and third-party consumers have accurate, executable representations of API capabilities.
- **Consequence:** Out-of-date API documents result in frontend integration failures, broken client apps, and excessive developer communication.

### DOC-003: Diagrams as Code
All system architecture, network topologies, database ERDs, and sequence flows MUST be documented using text-based diagram formats (e.g., Mermaid.js or PlantUML) embedded directly in markdown files. Binary image files (`.png`, `.jpg`) of design drawings are prohibited for custom diagrams.
- **Rationale:** Text-based diagrams can be diffed, searched, and updated directly via git, ensuring they do not rot.
- **Consequence:** Storing binary screenshot images of design diagrams makes editing them impossible when the layout changes, leading to stale documentation.

### DOC-004: Explaining "Why", Not "What"
Inline code comments MUST NOT describe what a line of code does. Comments MUST only explain *why* code was written in a non-obvious way (e.g., workarounds for third-party bugs, performance trade-offs, or complex business algorithms).
- **Rationale:** Code syntax is self-documenting for competent developers; comments that restate the code bloat the file and rot when code is updated.
- **Consequence:** Comments like `// Increment i by 1` add clutter and quickly become out of sync during code changes.

### DOC-005: Unified Project Changelog
Every repository MUST maintain a `CHANGELOG.md` file following the **Keep a Changelog** standard. Changes must be cataloged under: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, or `Security`.
- **Rationale:** Provides a human-readable list of changes per release version so developers can verify impact before upgrading.
- **Consequence:** Committing without a changelog forces operations and downstream teams to scan raw commit logs to find release details.

---

## Recommended Practices

### DOC-050: Public API XML Comments
In backend code, all public-facing API controller actions, interface definitions, and DTOs SHOULD include descriptive XML documentation comments.
- **Rationale:** Feeds directly into OpenAPI generators, creating self-documenting interactive sandboxes.

---

## Anti-patterns

### Stale Documentation ("Doc Rot")
Leaving design documents unchanged after rewriting the implementation.
- **Why it's harmful:** Misleads developers, causes onboarding friction, and leads to incorrect integration assumptions.
- **What to do instead:** Treat documentation updates as an integral part of the pull request criteria.

### Obvious Comments
```csharp
// ❌ Anti-pattern: Stating the obvious
public int Id { get; set; } // The unique identifier of the entity
```
- **Why it's harmful:** Visual noise that hides important code and adds maintenance overhead.
- **What to do instead:** Delete the comment. If the property name is unclear, rename it.

---

## Examples

### ✅ Correct (Mermaid diagram, clear ADR, "Why" comment)

**ADR File (`docs/adr/0004-use-redis-for-idempotency.md`):**
```markdown
# 4. Use Redis for Payment Idempotency Keys

* Date: 2026-07-14
* Status: Accepted
* Decided by: Architecture Board

## Context
Payment checkout requests are prone to network retries, causing duplicate charges. We need a cache mechanism to validate `X-Idempotency-Key` headers.

## Decision
We will use Redis as a distributed cache to store idempotency keys. Keys will have a Time-to-Live (TTL) of 120 seconds.

## Consequences
- Requires Redis infrastructure in production.
- Simplifies application logic by checking headers in middleware.
- Eliminates duplicate payment charges.
```

**Non-obvious "Why" Comment:**
```csharp
// We enforce a 15-minute wait time to satisfy local bank clearing regulations, 
// which require manual review of orders flagged for suspicious transaction patterns.
if (order.IsFlagged && order.LastReviewTime > DateTime.UtcNow.AddMinutes(-15))
{
    throw new ComplianceException(ErrorCode.COMPLIANCE_HOLD);
}
```

### ❌ Incorrect (Obvious comment, binary screenshot diagram)

```csharp
// ❌ Anti-pattern: "What" comment, no context
// If order status is pending, set it to processing
if (order.Status == OrderStatus.Pending) 
{
    order.Status = OrderStatus.Processing;
}
```

---

## Checklist
- [ ] Are all major architectural decisions documented via sequential ADR files?
- [ ] Did you update the Swagger/OpenAPI specifications for any API modifications?
- [ ] Are all system sequence or architecture diagrams written in text-based Mermaid format?
- [ ] Do inline comments focus exclusively on explaining the "Why" behind complex decisions?
- [ ] Has the `CHANGELOG.md` file been updated for the current version release?
