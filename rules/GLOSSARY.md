# Glossary of Terms

> **Applies To:** All Conventions
> **Last Updated:** 2025-07-14

This glossary defines terminology used across all convention documents in the Antigravity Rule System. Each term includes a concise definition and, where applicable, a reference to the convention that owns the detailed rules for that concept.

Use this glossary to ensure consistent language across documentation, code reviews, and AI agent interactions.

---

## How to Read This Glossary

- **Term** — The canonical name. Use this exact term in all convention documents.
- **Definition** — A concise, unambiguous explanation.
- **See Also** — The convention(s) that own detailed rules for this concept.

---

## Architecture Terms

| Term | Definition | See Also |
|---|---|---|
| **Boundary** | A well-defined interface between two layers, modules, or systems. Dependencies MUST flow inward (toward the domain) and never outward. | 02 — Architecture |
| **Clean Architecture** | An architecture style where the Domain layer has zero external dependencies, surrounded by Application, Infrastructure, and Presentation layers with strict dependency direction. | 02 — Architecture |
| **Cross-Cutting Concern** | Functionality that spans multiple layers or modules (e.g., logging, authentication, caching). Typically handled via middleware, decorators, or aspect-oriented techniques. | 02 — Architecture, 05 — Error Handling |
| **Dependency** | A relationship where one module requires another to function. Dependencies MUST point toward abstractions, not implementations (Dependency Inversion Principle). | 01 — Engineering Standards, 14 — Dependency Mgmt |
| **Dependency Injection (DI)** | A technique where an object's dependencies are provided externally rather than created internally. Required for testability and adherence to SOLID. | 01 — Engineering Standards, `extensions/aspnet-conventions.md` |
| **Domain Layer** | The innermost layer containing business entities, value objects, domain events, and business rules. Has no dependencies on other layers or external frameworks. | 02 — Architecture |
| **Application Layer** | Contains use cases, application services, DTOs, and interfaces for infrastructure. Depends only on the Domain layer. | 02 — Architecture |
| **Infrastructure Layer** | Implements interfaces defined in the Application layer (e.g., repositories, email services, external API clients). Depends on Application and Domain layers. | 02 — Architecture |
| **Presentation Layer** | The outermost layer handling UI or API concerns (controllers, Blazor pages, API endpoints). Depends on Application layer. | 02 — Architecture |
| **Layer** | A logical grouping of related concerns within the architecture. Each layer has defined responsibilities and dependency rules. | 02 — Architecture |
| **Module** | A self-contained unit of functionality within a layer. Modules communicate through well-defined interfaces. | 02 — Architecture |
| **Monolith** | A deployment model where the entire application runs as a single process. May still have clean internal architecture. | 02 — Architecture |

---

## Design Pattern Terms

| Term | Definition | See Also |
|---|---|---|
| **Repository** | A pattern that encapsulates data access logic and exposes a collection-like interface for domain entities. The Application layer defines the interface; Infrastructure implements it. | 07 — Database |
| **Service** | A stateless class that encapsulates a specific business operation or use case. Application Services orchestrate domain logic; Domain Services contain domain logic that doesn't belong to a single entity. | 02 — Architecture, 03 — Coding Standards |
| **DTO (Data Transfer Object)** | A plain object used to transfer data between layers or across network boundaries. Contains no business logic. | 02 — Architecture, 08 — API Design |
| **Factory** | A pattern that encapsulates object creation logic, useful when construction is complex or requires choosing among multiple implementations. | 03 — Coding Standards |
| **Strategy** | A pattern where a family of algorithms is encapsulated behind a common interface, allowing the algorithm to vary independently of clients. | 03 — Coding Standards |
| **Observer** | A pattern where an object (subject) notifies a list of dependents (observers) when its state changes. SignalR hub patterns often use this. | 03 — Coding Standards |
| **Mediator** | A pattern that reduces coupling by having objects communicate through a central mediator rather than directly. Often implemented via MediatR in .NET. | 03 — Coding Standards |
| **Decorator** | A pattern that wraps an object to extend its behavior without modifying the original class. Used for cross-cutting concerns like caching, logging, or retry logic. | 03 — Coding Standards |
| **Specification** | A pattern that encapsulates a business rule into a reusable, composable object. Commonly used for query filtering. | 03 — Coding Standards, 07 — Database |
| **Unit of Work** | A pattern that maintains a list of objects affected by a business transaction and coordinates writing out changes. EF Core `DbContext` implements this. | 07 — Database |
| **CQRS (Command Query Responsibility Segregation)** | A pattern that separates read models from write models, allowing each to be optimized independently. | 02 — Architecture, 08 — API Design |
| **Value Object** | An immutable domain object that is defined by its attributes rather than a unique identity (e.g., `Money`, `Address`). | 02 — Architecture |
| **Entity** | A domain object with a unique identity that persists over time, even when its attributes change. | 02 — Architecture |
| **Aggregate** | A cluster of domain objects treated as a single unit for data changes. Has a root entity that controls access. | 02 — Architecture |
| **Domain Event** | A record of something meaningful that happened in the domain. Used to decouple side effects from core business operations. | 02 — Architecture |

---

## Process Terms

| Term | Definition | See Also |
|---|---|---|
| **CI (Continuous Integration)** | The practice of automatically building and testing code every time a change is pushed to the repository. | 13 — Version Control & CI/CD |
| **CD (Continuous Delivery/Deployment)** | The practice of automatically deploying code to staging or production environments after it passes CI. | 13 — Version Control & CI/CD, 16 — DevOps |
| **Migration** | A versioned change to the database schema. Migrations MUST be forward-only in production and reversible in development. | 07 — Database |
| **Rollback** | The process of reverting a deployment or migration to a previous known-good state. | 07 — Database, 16 — DevOps |
| **Pull Request (PR)** | A request to merge code changes into a target branch, accompanied by description, linked issues, and reviewer assignments. | 13 — Version Control & CI/CD |
| **Code Review** | The process of examining code changes for correctness, style, security, and convention compliance before merging. | 13 — Version Control & CI/CD |
| **Feature Branch** | A short-lived branch created for developing a single feature or fix. Merged back to the main branch via pull request. | 13 — Version Control & CI/CD |
| **Hotfix** | An urgent code change applied directly to production to resolve a critical defect. Follows an expedited review process. | 13 — Version Control & CI/CD, 16 — DevOps |
| **Release** | A tagged, versioned snapshot of the codebase that is deployed to production. | 13 — Version Control & CI/CD |
| **Sprint** | A time-boxed iteration (typically 1–4 weeks) during which a set of work items is completed. | — |

---

## Quality Terms

| Term | Definition | See Also |
|---|---|---|
| **Code Coverage** | The percentage of code lines, branches, or paths exercised by automated tests. A metric, not a goal — high coverage does not guarantee correctness. | 09 — Testing |
| **Technical Debt** | The accumulated cost of shortcuts, workarounds, or deferred improvements in the codebase. MUST be tracked and prioritized. | 01 — Engineering Standards |
| **Code Smell** | A surface-level indicator that something may be wrong in the code (e.g., long methods, deep nesting, duplicated logic). Not a bug, but a sign of possible deeper issues. | 03 — Coding Standards |
| **Cyclomatic Complexity** | A quantitative measure of the number of linearly independent paths through a function. Lower is better; high values indicate methods that are hard to test and maintain. | 03 — Coding Standards |
| **Linting** | Automated static analysis that checks code for style violations, potential errors, and convention compliance. | 03 — Coding Standards |
| **Static Analysis** | Examination of code without executing it, used to detect bugs, security vulnerabilities, and convention violations. | 03 — Coding Standards, 06 — Security |
| **Refactoring** | Restructuring existing code without changing its external behavior. SHOULD be done continuously, not deferred. | 03 — Coding Standards |
| **Anti-pattern** | A commonly used approach that appears correct but leads to poor outcomes. Each convention documents relevant anti-patterns. | All conventions |

---

## Security Terms

| Term | Definition | See Also |
|---|---|---|
| **Authentication (AuthN)** | The process of verifying *who* a user or system is (e.g., via JWT token, OAuth, username/password). | 06 — Security |
| **Authorization (AuthZ)** | The process of determining *what* an authenticated user is allowed to do. | 06 — Security |
| **RBAC (Role-Based Access Control)** | An authorization model where permissions are assigned to roles, and roles are assigned to users. | 06 — Security |
| **JWT (JSON Web Token)** | A compact, URL-safe token format used for authentication and authorization. Contains claims about the user. | 06 — Security, `extensions/aspnet-conventions.md` |
| **CORS (Cross-Origin Resource Sharing)** | A browser security mechanism that controls which domains can make requests to your API. | 06 — Security, 08 — API Design |
| **SQL Injection** | An attack where malicious SQL is injected through user input. Prevented by parameterized queries and ORM usage. | 06 — Security, 07 — Database |
| **XSS (Cross-Site Scripting)** | An attack where malicious scripts are injected into web pages viewed by other users. Prevented by output encoding and Content Security Policy. | 06 — Security, 10 — UI/UX |
| **CSRF (Cross-Site Request Forgery)** | An attack that tricks authenticated users into submitting unintended requests. Prevented by anti-forgery tokens. | 06 — Security |
| **Secret** | Any sensitive configuration value (API keys, connection strings, passwords) that MUST NOT be committed to source control. | 06 — Security, 16 — DevOps |
| **Input Validation** | The process of verifying that user input meets expected format, type, length, and range before processing. First line of defense against injection attacks. | 06 — Security, 08 — API Design |
| **Principle of Least Privilege** | Users and services SHOULD be granted the minimum permissions necessary to perform their tasks. | 06 — Security |
| **Data at Rest** | Data stored in databases, file systems, or backups. SHOULD be encrypted for sensitive information. | 06 — Security, 07 — Database |
| **Data in Transit** | Data being transmitted over a network. MUST be encrypted using TLS/HTTPS. | 06 — Security |

---

## UI/UX Terms

| Term | Definition | See Also |
|---|---|---|
| **Component** | A self-contained, reusable UI building block with its own markup, styling, and behavior. The fundamental unit of UI construction. | 10 — UI/UX, `extensions/blazor-conventions.md` |
| **Design Token** | A named value (color, spacing, font size, etc.) that represents a design decision. Stored as CSS custom properties or constants. | 10 — UI/UX |
| **Design System** | A collection of reusable components, design tokens, and guidelines that ensure visual consistency across the application. | 10 — UI/UX |
| **BEM (Block Element Modifier)** | A CSS naming methodology that uses the pattern `block__element--modifier` to create predictable, non-conflicting class names. | 10 — UI/UX, `extensions/blazor-conventions.md` |
| **Responsive Design** | An approach where UI adapts to different screen sizes and devices using flexible layouts, media queries, and relative units. | 10 — UI/UX |
| **Accessibility (a11y)** | The practice of making UI usable by people with disabilities, including screen reader support, keyboard navigation, and sufficient color contrast. | 10 — UI/UX |
| **Layout Component** | A component responsible for page structure and content arrangement (e.g., grid, sidebar, header) rather than specific business functionality. | 10 — UI/UX |
| **Presentational Component** | A component that receives data via parameters and renders UI without managing state or business logic. | 10 — UI/UX, `extensions/blazor-conventions.md` |
| **Container Component** | A component that manages state, calls services, and passes data to presentational components. | 10 — UI/UX, `extensions/blazor-conventions.md` |
| **SignalR** | A library for real-time web communication. Used for push notifications, live updates (e.g., KDS order updates). | `extensions/blazor-conventions.md` |
| **SPA (Single-Page Application)** | A web application that loads a single HTML page and dynamically updates content without full page reloads. | 10 — UI/UX |

---

## Testing Terms

| Term | Definition | See Also |
|---|---|---|
| **Unit Test** | A test that verifies a single unit of work (method, function, class) in isolation from all external dependencies. | 09 — Testing |
| **Integration Test** | A test that verifies the interaction between two or more components, often including real infrastructure (database, file system). | 09 — Testing |
| **End-to-End Test (E2E)** | A test that exercises the full application stack from the user interface to the database, simulating real user behavior. | 09 — Testing |
| **Mock** | A test double that records interactions and can assert that specific methods were called with expected arguments. Used to isolate the system under test. | 09 — Testing |
| **Stub** | A test double that provides canned responses to method calls. Simpler than a mock — does not verify interactions. | 09 — Testing |
| **Fake** | A test double with a simplified working implementation (e.g., an in-memory repository instead of a real database). | 09 — Testing |
| **Fixture** | Reusable setup code or data that establishes a known state before tests run. | 09 — Testing |
| **Arrange-Act-Assert (AAA)** | A pattern for structuring unit tests: set up preconditions (Arrange), execute the action (Act), verify the outcome (Assert). | 09 — Testing |
| **Test Pyramid** | A model recommending many unit tests, fewer integration tests, and even fewer E2E tests. Higher layers are slower and more brittle. | 09 — Testing |
| **SUT (System Under Test)** | The specific class, method, or component being tested in a given test case. | 09 — Testing |
| **Test Coverage** | See *Code Coverage*. | 09 — Testing |
| **Regression Test** | A test that verifies previously working functionality has not been broken by new changes. | 09 — Testing |
| **Smoke Test** | A minimal set of tests run after deployment to verify the application starts and core functionality works. | 09 — Testing, 16 — DevOps |
| **Load Test** | A test that measures system behavior under expected or peak load to identify performance bottlenecks. | 11 — Performance |
| **Mutation Testing** | A technique where small changes (mutations) are introduced into the code to verify that tests detect them. Measures test effectiveness beyond coverage. | 09 — Testing |

---

## Infrastructure & DevOps Terms

| Term | Definition | See Also |
|---|---|---|
| **Environment** | A deployment target (e.g., Development, Staging, Production) with its own configuration, database, and access rules. | 16 — DevOps |
| **Container** | A lightweight, portable runtime environment (e.g., Docker) that packages an application with its dependencies. | 16 — DevOps |
| **Orchestration** | The automated management of containers across multiple hosts (e.g., Kubernetes, Docker Compose). | 16 — DevOps |
| **Health Check** | An endpoint or script that reports whether the application is running and can serve requests. | 16 — DevOps, 08 — API Design |
| **Blue-Green Deployment** | A deployment strategy using two identical environments, switching traffic from one to the other to achieve zero-downtime releases. | 16 — DevOps |
| **Feature Flag** | A configuration toggle that enables or disables functionality at runtime without deploying new code. | 16 — DevOps, 03 — Coding Standards |
| **Infrastructure as Code (IaC)** | The practice of managing infrastructure through version-controlled configuration files rather than manual processes. | 16 — DevOps |
| **Pipeline** | An automated sequence of steps (build, test, deploy) triggered by code changes. | 13 — Version Control & CI/CD |

---

## Data & Database Terms

| Term | Definition | See Also |
|---|---|---|
| **Schema** | The structure of a database including tables, columns, relationships, indexes, and constraints. | 07 — Database |
| **Index** | A database structure that improves query performance by allowing faster row lookup at the cost of additional storage and write overhead. | 07 — Database, 11 — Performance |
| **Foreign Key** | A constraint that enforces referential integrity between two tables. | 07 — Database |
| **Stored Procedure** | A precompiled SQL routine stored in the database. Use sparingly; prefer application-layer logic. | 07 — Database, `extensions/sqlserver-conventions.md` |
| **Seed Data** | Initial data loaded into the database for development, testing, or application bootstrapping. | 07 — Database |
| **Soft Delete** | A pattern where records are marked as deleted (e.g., `IsDeleted = true`) rather than physically removed, preserving audit history. | 07 — Database |
| **Audit Trail** | A record of who changed what data and when. Typically implemented via shadow properties or a dedicated audit table. | 07 — Database, 06 — Security |
| **Connection Pooling** | Reusing database connections across requests to reduce the overhead of establishing new connections. | 07 — Database, 11 — Performance |
| **ORM (Object-Relational Mapper)** | A framework (e.g., Entity Framework Core) that maps between database tables and application objects, reducing manual SQL. | 07 — Database |
| **N+1 Query Problem** | A performance anti-pattern where a query fetches N parent records, then issues a separate query for each parent's children. Solved by eager loading or batching. | 07 — Database, 11 — Performance |

---

## General Engineering Terms

| Term | Definition | See Also |
|---|---|---|
| **SOLID** | Five principles of object-oriented design: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion. | 01 — Engineering Standards |
| **DRY (Don't Repeat Yourself)** | A principle stating that every piece of knowledge should have a single, unambiguous representation in the system. | 01 — Engineering Standards |
| **KISS (Keep It Simple, Stupid)** | A principle favoring simple solutions over complex ones. Complexity SHOULD be introduced only when justified. | 01 — Engineering Standards |
| **YAGNI (You Ain't Gonna Need It)** | A principle advising against implementing functionality until it is actually needed. | 01 — Engineering Standards |
| **Abstraction** | The process of hiding implementation details behind a well-defined interface. Essential for Dependency Inversion. | 01 — Engineering Standards |
| **Cohesion** | The degree to which elements within a module belong together. High cohesion is desirable. | 02 — Architecture |
| **Coupling** | The degree of interdependence between modules. Low (loose) coupling is desirable. | 02 — Architecture |
| **Idempotent** | An operation that produces the same result regardless of how many times it is executed (e.g., HTTP PUT, DELETE). | 08 — API Design |
| **Backward Compatibility** | The ability of a system to work with older versions of interfaces, data formats, or APIs. | 08 — API Design, 07 — Database |
| **Resource File** | A file (e.g., `.resx`, `.json`) that stores externalized strings, messages, and labels for localization and separation of concerns. | 15 — Localization |
| **Magic String / Magic Number** | A hardcoded literal value embedded in logic code without explanation. Prohibited — MUST be replaced with named constants or resource files. | 01 — Engineering Standards, 15 — Localization |
| **Convention over Configuration** | A design philosophy that reduces decisions by providing sensible defaults, requiring explicit configuration only when deviating from the norm. | 01 — Engineering Standards |

---

> [!NOTE]
> This glossary is a living document. When a new term is introduced in any convention, it SHOULD be added here with its definition and cross-reference. Terms MUST be used consistently across all convention documents — always prefer the exact term name listed in this glossary.
