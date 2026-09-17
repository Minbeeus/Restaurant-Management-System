# API Design Convention

> **Rule Hierarchy Level:** 04
> **Applies To:** Backend
> **Last Updated:** 2025-07-14

## Purpose

This document defines the mandatory standards for designing, structuring, and consuming RESTful APIs across the system. It ensures every endpoint follows a predictable, consistent contract so that frontend clients, mobile apps, and third-party integrators can rely on uniform behavior. All rules enforce SOLID principles at the API boundary — particularly the Interface Segregation Principle (small, focused endpoints) and the Dependency Inversion Principle (clients depend on stable abstractions, not implementation details).

## Scope

This convention covers: URL structure, HTTP method usage, status codes, versioning, request/response envelope, pagination, filtering, sorting, idempotency, rate limiting, and error response format.

This convention does **NOT** cover: authentication/authorization flows (see **Security Convention**), real-time communication protocols such as WebSocket/SignalR (see **Real-Time Convention**), or domain entity modeling (see **Domain Modeling Convention**). Input validation rule definitions belong in the **Validation Convention** — this document only specifies *where* validation occurs at the API boundary.

## Principles

1. **Uniform Interface** — Every API endpoint MUST follow the same URL structure, response envelope, and error format. Predictability reduces integration cost and eliminates guesswork.
2. **Resource Orientation** — APIs represent *resources* (nouns), not *operations* (verbs). HTTP methods express the operation. This aligns with REST semantics and enables caching, proxying, and tooling.
3. **Fail Fast, Fail Clearly** — Invalid requests MUST be rejected at the API boundary with machine-readable error codes and human-readable messages sourced from resource files. Never let malformed input reach the domain layer.
4. **Backward Compatibility** — Once published, an API version is a contract. Non-breaking changes are additive only. Breaking changes require a new version.
5. **Defense in Depth** — Every state-changing endpoint MUST be idempotent-safe and rate-limited. The API layer is the first line of defense against duplicate submissions and abuse.

---

## Mandatory Rules

### RESTful Resource Naming

**API-001** — Resource URLs MUST use plural nouns for collection endpoints.
- *Rationale:* Plural nouns (`/orders`, `/menu-items`) create a consistent mental model — the URL names the collection, the HTTP method names the action.
- *Consequence:* Endpoints using singular nouns or verbs will be rejected in code review.

**API-002** — Multi-word resource names MUST use lowercase kebab-case.
- *Rationale:* URLs are case-sensitive in RFC 3986. Kebab-case is the most readable and universally supported convention.
- *Consequence:* Endpoints using camelCase, PascalCase, or snake_case in URL segments will be rejected.

**API-003** — URLs MUST NOT contain verbs describing the action.
- *Rationale:* The HTTP method already conveys the action. Verbs in URLs create redundancy and inconsistency (`POST /create-order` vs. `POST /orders`).
- *Consequence:* Endpoints with verbs like `get`, `create`, `update`, `delete` in the path will be rejected.

**API-004** — Nested resources MUST be used to express parent-child relationships, limited to one level of nesting.
- *Rationale:* `/orders/{orderId}/items` clearly expresses that items belong to an order. Deeper nesting (more than two resources) creates brittle, hard-to-maintain URLs.
- *Consequence:* URLs with more than one level of nesting MUST be refactored to top-level resources with query parameter filters.

**API-005** — Non-CRUD operations on a resource MUST use the pattern `POST /api/v{n}/{resource}/{id}/{action}`.
- *Rationale:* Some business operations (cancel, approve, submit) are not cleanly mapped to PUT/PATCH. A sub-resource action endpoint keeps the URL resource-oriented while accommodating domain actions.
- *Consequence:* RPC-style endpoints (`/api/v1/cancelOrder`) will be rejected.

### HTTP Methods & Status Codes

**API-006** — Each HTTP method MUST be used according to its defined semantics.
- `GET` — Retrieve resource(s). MUST be safe and idempotent. MUST NOT modify state.
- `POST` — Create a new resource or trigger a non-CRUD action.
- `PUT` — Full replacement of a resource. Client sends the complete representation.
- `PATCH` — Partial update of a resource. Client sends only changed fields.
- `DELETE` — Remove a resource (or soft-delete per domain rules).
- *Rationale:* Correct method usage enables HTTP caching, proxy optimization, and client-side safety assumptions.
- *Consequence:* Using `GET` to modify data or `POST` for idempotent retrieval will be treated as a critical defect.

**API-007** — Response status codes MUST follow this mapping table:

| Status Code | Name                 | When to Use                                                                 |
|-------------|----------------------|-----------------------------------------------------------------------------|
| `200`       | OK                   | Successful `GET`, `PUT`, `PATCH`, or `DELETE` operations                    |
| `201`       | Created              | Successful `POST` that creates a new resource                               |
| `204`       | No Content           | Successful `DELETE` when no response body is returned                       |
| `400`       | Bad Request          | Validation failure, malformed input, or business rule rejection             |
| `401`       | Unauthorized         | Missing or expired authentication credentials                              |
| `403`       | Forbidden            | Authenticated but lacks required permissions                                |
| `404`       | Not Found            | Requested resource does not exist                                           |
| `409`       | Conflict             | State conflict (e.g., concurrent modification, duplicate unique constraint) |
| `422`       | Unprocessable Entity | Syntactically valid but semantically invalid request                        |
| `429`       | Too Many Requests    | Rate limit exceeded                                                         |
| `500`       | Internal Server Error| Unhandled system exception (caught by global middleware)                    |

- *Rationale:* Consistent status codes allow clients to implement generic error handling without inspecting response bodies for every endpoint.
- *Consequence:* Returning `200` for error conditions or `500` for validation failures will be rejected.

### Versioning Strategy

**API-008** — API versioning MUST use URL path versioning in the format `/api/v{n}/...`.
- *Rationale:* URL path versioning is explicit, visible in logs and documentation, and requires no custom headers. It is the simplest strategy for teams to adopt.
- *Consequence:* APIs without version prefix or using header-based versioning will be rejected.

**API-009** — A new major version MUST be created only for breaking changes. Non-breaking changes MUST be applied to the current version.
- Breaking changes include: removing a field, renaming a field, changing a field type, removing an endpoint, changing URL structure.
- Non-breaking changes include: adding a new optional field, adding a new endpoint, adding a new optional query parameter.
- *Rationale:* Version proliferation increases maintenance burden. Only break the contract when unavoidable.
- *Consequence:* Adding a new optional field under a new version without justification will be rejected.

**API-010** — Deprecated API versions MUST return a `Deprecation` response header with the sunset date and MUST remain available for a minimum of 90 days after deprecation announcement.
- *Rationale:* Clients need time to migrate. Abrupt removal breaks integrations.
- *Consequence:* Removing an API version without the deprecation period will be treated as a production incident.

### Request/Response Format

**API-011** — All API responses MUST use the unified response envelope:
```json
{
  "success": true | false,
  "message": "Human-readable message from resource file",
  "data": { },
  "errors": [],
  "traceId": "W3C Trace Context ID"
}
```
- `success` — Boolean indicating operation result.
- `message` — Localized, human-readable message sourced from a resource file or constants class. MUST NOT be hardcoded inline.
- `data` — Payload for successful responses. `null` for error responses.
- `errors` — Array of machine-readable error codes for failed responses. Empty array for success.
- `traceId` — W3C Trace Context identifier extracted from the request context. MUST be present on every error response.
- *Rationale:* A unified envelope lets clients write one deserialization path for all endpoints.
- *Consequence:* Endpoints returning raw data without the envelope will be rejected.

**API-012** — API endpoints MUST NOT expose domain entities directly. All input and output MUST use Data Transfer Objects (DTOs).
- *Rationale:* Exposing domain entities leaks internal structure, creates tight coupling between API consumers and the domain model, and violates the Dependency Inversion Principle.
- *Consequence:* Returning a domain entity or database model from a controller will be treated as a critical defect.

**API-013** — Request validation MUST occur at the API boundary before any business logic executes.
- *Rationale:* Fail fast. Rejecting invalid input early avoids wasted computation and ensures the domain layer receives only valid data (see **Validation Convention**).
- *Consequence:* Validation logic embedded in domain services that should be caught at the API layer will be flagged.

### Pagination, Filtering, Sorting

**API-014** — Collection endpoints returning lists MUST support pagination using query parameters `pageIndex` and `pageSize`.
- Default `pageIndex`: `1` (1-based indexing).
- Default `pageSize`: `10`.
- Maximum `pageSize`: `100`.
- *Rationale:* Unbounded queries can crash databases and saturate network bandwidth.
- *Consequence:* Collection endpoints without pagination will be rejected.

**API-015** — Paginated responses MUST return the following metadata structure inside the `data` field:
```json
{
  "items": [],
  "pageIndex": 1,
  "pageSize": 10,
  "totalItems": 245,
  "totalPages": 25,
  "hasNextPage": true
}
```
- *Rationale:* Clients need total counts and navigation hints to build pagination UI without additional requests.
- *Consequence:* Returning a bare array without pagination metadata will be rejected.

**API-016** — Filtering MUST use query parameters named after the filterable field. Sorting MUST use `sortBy` and `sortDirection` query parameters.
- Example: `GET /api/v1/orders?status=processing&sortBy=createdAt&sortDirection=desc`
- *Rationale:* Consistent parameter naming enables generic client-side table/grid components.
- *Consequence:* Non-standard filter or sort parameter names will be flagged in review.

### Idempotency

**API-017** — State-changing endpoints (`POST`, `PUT`, `PATCH`) for critical operations MUST require an `X-Idempotency-Key` header containing a client-generated UUID v4.
- *Rationale:* Network retries, user double-clicks, and unreliable connections can cause duplicate submissions. Idempotency keys prevent duplicate resource creation and duplicate payments.
- *Consequence:* Critical state-changing endpoints without idempotency support will not pass security review.

**API-018** — The backend MUST cache idempotency keys with a Time-To-Live (TTL) of 120 seconds minimum. Duplicate requests within the TTL window MUST return the original response without re-executing business logic.
- *Rationale:* Short TTL prevents memory bloat; 120 seconds covers typical retry windows.
- *Consequence:* Re-processing duplicate requests will be treated as a data integrity defect.

### Rate Limiting

**API-019** — All public-facing API endpoints MUST enforce rate limiting with the following default tiers:

| Endpoint Category       | Limit              |
|------------------------|--------------------|
| Read (GET)             | 120 requests/min   |
| Write (POST/PUT/PATCH) | 60 requests/min    |
| Authentication         | 10 requests/min    |
| File Upload            | 10 requests/min    |

- *Rationale:* Rate limiting protects against abuse, brute-force attacks, and accidental load spikes.
- *Consequence:* Endpoints without rate limiting will fail security review.

**API-020** — Rate-limited responses MUST include the following headers:
- `X-RateLimit-Limit` — Maximum requests allowed in the window.
- `X-RateLimit-Remaining` — Requests remaining in the current window.
- `X-RateLimit-Reset` — UTC epoch timestamp when the window resets.
- *Rationale:* Clients need these headers to implement backoff and throttling logic.
- *Consequence:* Missing rate limit headers will be flagged.

### Error Messages

**API-021** — All user-facing error messages in API responses MUST be sourced from resource files, localization systems, or dedicated constants classes. Hardcoded string literals in response construction are STRICTLY PROHIBITED.
- *Rationale:* Hardcoded messages prevent localization, create inconsistency, and make message changes require code deployment.
- *Consequence:* Any hardcoded error string found in controller or service code will be rejected in code review immediately.

**API-022** — Error codes in the `errors` array MUST be machine-readable UPPER_SNAKE_CASE identifiers (e.g., `VOUCHER_EXPIRED`, `INSUFFICIENT_STOCK`, `ORDER_ALREADY_CANCELLED`).
- *Rationale:* Machine-readable codes allow clients to map errors to specific UI behaviors without parsing human-readable text.
- *Consequence:* Using human-readable sentences as error codes will be rejected.

---

## Recommended Practices

**API-050** — Response DTOs SHOULD include a `self` link or resource URI for created resources.
- *Rationale:* Clients can follow the link to retrieve the full resource without constructing URLs.

**API-051** — `DELETE` operations SHOULD use soft-delete (setting an `isDeleted` flag) rather than physical deletion unless explicitly required by data retention policy.
- *Rationale:* Soft-delete preserves audit trails and enables undo operations.

**API-052** — Long-running operations SHOULD return `202 Accepted` with a status polling URL rather than blocking the client.
- *Rationale:* Prevents HTTP timeouts and improves perceived responsiveness.

**API-053** — API documentation SHOULD be auto-generated from code annotations and SHOULD be available at a well-known path (e.g., `/api/docs`).
- *Rationale:* Manual documentation drifts from implementation. Auto-generation keeps docs accurate.

**API-054** — Query parameter names SHOULD use camelCase to match JSON property naming conventions.
- *Rationale:* Consistency between URL parameters and JSON body properties reduces cognitive load.

---

## Anti-patterns

### 1. Verb-Stuffed URLs
- **Description:** URLs like `/api/v1/getAllActiveOrders` or `/api/v1/createMenuItem`.
- **Why it's harmful:** Breaks REST semantics, makes endpoints unpredictable, and prevents HTTP method-based caching.
- **What to do instead:** `GET /api/v1/orders?status=active` and `POST /api/v1/menu-items`.

### 2. Naked Domain Entity Exposure
- **Description:** Returning the ORM entity or domain model directly as the API response.
- **Why it's harmful:** Leaks internal database structure, navigation properties, and sensitive fields. Creates tight coupling — any domain model change breaks all consumers.
- **What to do instead:** Map domain entities to purpose-built DTOs at the API boundary.

### 3. Status Code Lying
- **Description:** Returning `200 OK` with `{ "success": false, "error": "Not found" }` instead of `404`.
- **Why it's harmful:** Breaks HTTP semantics, defeats client-side error handling middleware, and makes monitoring tools useless.
- **What to do instead:** Use correct HTTP status codes. The response body provides *additional* detail, not the *only* error signal.

### 4. Hardcoded Error Strings
- **Description:** Writing `return Error("The discount code has expired")` directly in controller code.
- **Why it's harmful:** Cannot be localized, inconsistent phrasing across endpoints, requires code deployment to fix typos.
- **What to do instead:** Reference a resource key: `return Error(ErrorMessages.Get(ErrorCodes.VOUCHER_EXPIRED))`.

### 5. Unbounded Collection Returns
- **Description:** `GET /api/v1/orders` returning all 50,000 orders without pagination.
- **Why it's harmful:** Crashes databases with large result sets, saturates network, and causes client-side memory issues.
- **What to do instead:** Enforce mandatory pagination with a maximum `pageSize` cap of 100.

### 6. Deep URL Nesting
- **Description:** `/api/v1/restaurants/{rId}/floors/{fId}/tables/{tId}/orders/{oId}/items`.
- **Why it's harmful:** Brittle URLs that break when relationships change. Difficult to cache and route.
- **What to do instead:** Flatten to `/api/v1/order-items?orderId={oId}` or limit nesting to one level.

---

## Examples

### ✅ Correct: RESTful Resource URLs
```
GET    /api/v1/orders                          # List all orders (paginated)
GET    /api/v1/orders/{id}                     # Get single order
POST   /api/v1/orders                          # Create new order
PUT    /api/v1/orders/{id}                     # Full update
PATCH  /api/v1/orders/{id}                     # Partial update (e.g., status change)
DELETE /api/v1/orders/{id}                     # Delete order

GET    /api/v1/orders/{id}/items               # List items in an order
POST   /api/v1/orders/{id}/cancel              # Non-CRUD action: cancel order
POST   /api/v1/orders/{id}/submit              # Non-CRUD action: submit order
```

### ❌ Incorrect: Verb-Based and Inconsistent URLs
```
GET    /api/v1/getOrders                       # Verb in URL
POST   /api/v1/orders/create-new-order         # Redundant verb
POST   /api/v1/cancelOrder/123                 # RPC-style, not resource-oriented
GET    /api/v1/Order                           # Singular, PascalCase
GET    /api/v1/menu_items                      # snake_case instead of kebab-case
```

### ✅ Correct: Unified Success Response
```json
{
  "success": true,
  "message": "Order created successfully.",
  "data": {
    "id": "ord_2025071400001",
    "totalAmount": 150000.00,
    "status": "Processing",
    "createdAt": "2025-07-14T10:30:00Z"
  },
  "errors": [],
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

### ✅ Correct: Unified Error Response
```json
{
  "success": false,
  "message": "The discount code has expired or does not exist.",
  "data": null,
  "errors": [
    "VOUCHER_EXPIRED"
  ],
  "traceId": "00-7a8b9c0d1e2f3a4b5c6d7e8f9a0b1c2d-1a2b3c4d5e6f7a8b-01"
}
```

### ✅ Correct: Paginated Collection Response
```
GET /api/v1/orders?pageIndex=2&pageSize=10&status=processing&sortBy=createdAt&sortDirection=desc
```
```json
{
  "success": true,
  "message": "Query executed successfully.",
  "data": {
    "items": [
      { "id": "ord_001", "status": "Processing", "totalAmount": 85000.00 },
      { "id": "ord_002", "status": "Processing", "totalAmount": 120000.00 }
    ],
    "pageIndex": 2,
    "pageSize": 10,
    "totalItems": 45,
    "totalPages": 5,
    "hasNextPage": true
  },
  "errors": [],
  "traceId": "00-abc123def456abc123def456abc12345-1234567890abcdef-01"
}
```

### ✅ Correct: Idempotent Request
```http
POST /api/v1/orders HTTP/1.1
Content-Type: application/json
Authorization: Bearer eyJhbG...
X-Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "tableId": "tbl_05",
  "items": [
    { "menuItemId": "mi_042", "quantity": 2 }
  ]
}
```
*If the same `X-Idempotency-Key` is sent again within 120 seconds, the server returns the original `201` response without creating a duplicate order.*

### ✅ Correct: Rate Limit Headers
```http
HTTP/1.1 200 OK
X-RateLimit-Limit: 120
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1752480660
Content-Type: application/json
```

### ❌ Incorrect: Rate Limit Exceeded (without proper headers)
```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/json

{
  "success": false,
  "message": "Rate limit exceeded. Please retry after 45 seconds.",
  "data": null,
  "errors": ["RATE_LIMIT_EXCEEDED"],
  "traceId": "00-..."
}
```
*This response is correct in body but MUST also include `X-RateLimit-*` headers.*

### ✅ Correct: Error Messages from Resource Files (Pseudocode)
```
// ✅ Error message sourced from resource/constants
errorMessage = ErrorMessages.Get("VOUCHER_EXPIRED")
return BadRequest(ApiResponse.Fail(errorMessage, "VOUCHER_EXPIRED"))

// ❌ NEVER do this — hardcoded string
return BadRequest(ApiResponse.Fail("The voucher has expired", "VOUCHER_EXPIRED"))
```

### ✅ Correct: Deprecation Header
```http
HTTP/1.1 200 OK
Deprecation: Sun, 14 Oct 2025 00:00:00 GMT
Sunset: Sun, 14 Jan 2026 00:00:00 GMT
Link: </api/v2/orders>; rel="successor-version"
```

---

## Checklist

- □ All resource URLs use plural nouns in lowercase kebab-case
- □ No verbs appear in any URL path segment
- □ Nested resources are limited to one level of depth
- □ Non-CRUD actions use `POST /resource/{id}/{action}` pattern
- □ HTTP methods match their semantic purpose (GET is safe, POST creates)
- □ Status codes match the mapping table — no `200` for errors
- □ All endpoints are prefixed with `/api/v{n}/`
- □ Breaking changes result in a new API version; additive changes stay in current version
- □ Every response uses the unified envelope: `{ success, message, data, errors, traceId }`
- □ No domain entity is directly returned from any endpoint — DTOs only
- □ Request validation occurs at the API boundary before business logic
- □ Collection endpoints enforce pagination with `pageIndex`, `pageSize`, and max cap of 100
- □ Paginated responses include `items`, `pageIndex`, `pageSize`, `totalItems`, `totalPages`, `hasNextPage`
- □ Filtering uses field-name query parameters; sorting uses `sortBy` and `sortDirection`
- □ Critical `POST`/`PUT`/`PATCH` endpoints require `X-Idempotency-Key` header
- □ Idempotency keys are cached with a TTL of at least 120 seconds
- □ Rate limiting is applied to all public endpoints with tiered limits
- □ Rate limit responses include `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset`
- □ All error messages are sourced from resource files or constants — zero hardcoded strings
- □ Error codes are machine-readable `UPPER_SNAKE_CASE` identifiers
- □ Deprecated APIs return `Deprecation` and `Sunset` headers
- □ API documentation is auto-generated and accessible
