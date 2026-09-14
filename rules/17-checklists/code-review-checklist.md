# Code Review Checklist

> **Rule Hierarchy Level:** 17
> **Applies To:** Senior Developers and Technical Reviewers
> **Last Updated:** 2026-07-14

This checklist defines the criteria that reviewers MUST verify before approving a Pull Request for integration into the main branch.

---

## 1. Functional Correctness
- [ ] **Requirements Met:** The change completely satisfies the acceptance criteria of the user story or ticket description.
- [ ] **Edge Cases Handled:** Code handles null values, boundary conditions, empty collections, and network dropouts gracefully.
- [ ] **Error Recovery:** User interfaces and APIs return helpful, localized error messages when things go wrong, rather than throwing raw system exceptions.

---

## 2. Design & Architecture
- [ ] **SOLID Principles:** Review that classes are highly cohesive (SRP), open for extension (OCP), and dependencies are injected through abstractions rather than concrete instances (DIP).
- [ ] **Interface Segregation:** Interfaces are small and focused. No client classes are forced to implement methods they do not require.
- [ ] **Zero Logic Leaks:** Database entity tracking, raw SQL queries, and framework-specific routing do not leak into the Domain or Application layers.
- [ ] **Componentization:** The UI elements are composed of modular components. No redundant HTML/CSS copy-pasting is present in views.

---

## 3. Security & Compliance
- [ ] **Access Control:** The proper authentication filters and Role-Based Access Controls (`[Authorize]`) are present on endpoints.
- [ ] **Validation Guard:** Input parameters are whitelisted and validated before hitting business services.
- [ ] **OWASP Defenses:** SQL queries are parameterized (via EF Core or Dapper), output parameters are encoded against XSS, and CSRF tokens are validated on mutate requests.

---

## 4. Performance & Resource Management
- [ ] **Query Efficiency:** Verified that database fetches do not cause N+1 query problems and utilize projections to select only necessary fields.
- [ ] **Asynchronous Operations:** All external calls (database, cache, HTTP) use async methods with propagated cancellation tokens.
- [ ] **Caching:** Expensive or static dataset queries leverage cache-aside patterns.
- [ ] **Resource Cleanup:** Streams, connections, and client sockets are properly disposed of using `using` statements.

---

## 5. Maintainability & Testability
- [ ] **Self-Documenting Code:** Code is readable, variable names explain their purpose, and comments clarify the "Why" rather than the "What".
- [ ] **Test Coverage:** All new business logic is covered by high-quality unit tests.
- [ ] **Zero Hardcoded Copy:** UI labels and validation messages are externalized to resource translation files.
