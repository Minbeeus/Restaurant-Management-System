# Security Convention

> **Rule Hierarchy Level:** 06
> **Applies To:** All
> **Last Updated:** 2025-07-14

## Purpose

This document defines mandatory security rules and recommended practices that govern authentication, authorization, data protection, input validation, secret management, and defense against common attack vectors. Every team member and AI agent MUST follow these rules to ensure the system is resilient against known threats and complies with industry security standards.

## Scope

This convention covers:
- Authentication and session management
- Authorization and access control
- Input validation and output sanitization
- Encryption and password management
- Secret and credential management
- OWASP Top 10 mitigation strategies
- Sensitive data logging policies

This convention does NOT cover:
- API endpoint design patterns — see **API Convention**
- General coding standards — see **Coding Convention**
- Infrastructure-level firewall or network security configurations
- Compliance-specific audit procedures (SOC 2, PCI-DSS) beyond general best practices

## Principles

1. **Defense in Depth** — Security MUST be layered. No single control should be the sole line of defense; combine authentication, authorization, input validation, and encryption at every boundary.
2. **Principle of Least Privilege** — Every user, service, and process MUST operate with the minimum permissions necessary to perform its function. Default to deny, grant explicitly.
3. **Zero Trust for Input** — All input from external sources (users, APIs, third-party systems) MUST be treated as untrusted. Validate and sanitize at every entry point.
4. **Secrets Are Never Code** — Credentials, keys, tokens, and connection strings MUST NOT exist in source code, configuration files committed to version control, or client-side bundles. Externalize all secrets.
5. **Fail Securely** — When an error or exception occurs, the system MUST default to a secure state. Never expose stack traces, internal paths, or system details to end users.

---

## Mandatory Rules

### Authentication

**SEC-001: Token-Based Authentication MUST Use Short-Lived Access Tokens**
- All systems MUST use token-based authentication (e.g., JWT) with a maximum access token lifetime appropriate for the use case (typically 15 minutes to 2 hours).
- **Rationale:** Short-lived tokens limit the window of exploitation if a token is compromised.
- **Consequence:** Tokens with excessive lifetimes will be flagged as a critical security finding.

**SEC-002: Refresh Tokens MUST Be Used for Session Extension**
- Long-lived sessions MUST be maintained through a separate refresh token mechanism, not by extending access token expiry.
- Refresh tokens MUST be single-use (rotated on each use) and stored server-side or in secure, HttpOnly cookies.
- **Rationale:** Refresh token rotation ensures that a stolen refresh token can only be used once before detection.
- **Consequence:** Systems that reuse refresh tokens are vulnerable to token replay attacks.

**SEC-003: Tokens MUST Be Stored Securely on the Client**
- Access tokens and refresh tokens MUST be stored in `HttpOnly`, `Secure`, and `SameSite=Strict` cookies when used in browser-based applications.
- Tokens MUST NOT be stored in `localStorage`, `sessionStorage`, or accessible JavaScript variables.
- **Rationale:** HttpOnly cookies prevent XSS attacks from reading tokens. Secure flag enforces HTTPS. SameSite prevents CSRF-based token theft.
- **Consequence:** Token exposure via client-side storage creates a critical XSS-to-account-takeover vulnerability.

**SEC-004: Authentication Failures MUST NOT Reveal System Details**
- Error messages for failed login attempts MUST be generic (e.g., a resource key like `Auth.InvalidCredentials`). The system MUST NOT indicate whether the username or the password was incorrect.
- **Rationale:** Specific error messages enable user enumeration attacks.
- **Consequence:** User enumeration allows targeted brute-force or credential-stuffing attacks.

**SEC-005: Session Tokens MUST Be Invalidated on Logout**
- When a user logs out, the system MUST invalidate all associated tokens (access and refresh) server-side.
- **Rationale:** Prevents continued access after explicit logout.
- **Consequence:** Active tokens after logout allow unauthorized session continuation.

### Authorization

**SEC-006: Role-Based Access Control (RBAC) MUST Be Enforced at the API Boundary**
- Every API endpoint MUST declare its required roles or permissions. Unauthenticated or unauthorized requests MUST be rejected before reaching business logic.
- **Rationale:** Enforcing at the boundary ensures no business logic executes without proper authorization.
- **Consequence:** Missing boundary enforcement allows privilege escalation.

```
✅ Correct — Authorization declared at controller/endpoint level:
[Authorize(Roles = "Admin,Manager")]
public async Task<Result> UpdateSystemConfig(UpdateConfigCommand command)

❌ Incorrect — Authorization checked inside business logic:
public async Task<Result> UpdateSystemConfig(UpdateConfigCommand command)
{
    if (currentUser.Role != "Admin") return Unauthorized(); // Too late
    ...
}
```

**SEC-007: Resource-Level Authorization MUST Be Performed for Data Access**
- Beyond role checks, the system MUST verify that the authenticated user has permission to access or modify the specific resource being requested.
- **Rationale:** Role-based access alone does not prevent a Manager from modifying another branch's data.
- **Consequence:** Missing resource-level checks lead to Insecure Direct Object Reference (IDOR) vulnerabilities.

**SEC-008: Authorization Logic MUST NOT Exist in the Presentation Layer**
- UI code MAY hide or disable elements based on roles for UX purposes, but this MUST NOT be the sole authorization mechanism. All authorization MUST be enforced server-side.
- **Rationale:** Client-side checks are trivially bypassed.
- **Consequence:** Relying on UI-only authorization is equivalent to having no authorization.

**SEC-009: Default Access MUST Be Deny**
- All endpoints and resources MUST default to requiring authentication. Public endpoints MUST be explicitly marked as anonymous.
- **Rationale:** An accidentally exposed endpoint with default-allow can leak sensitive data.
- **Consequence:** Open-by-default systems accumulate unprotected endpoints over time.

### Input Validation & Sanitization

**SEC-010: ALL External Input MUST Be Validated at the API Boundary**
- Every request payload, query parameter, header value, and path parameter MUST be validated using a dedicated validation layer before entering business logic.
- **Rationale:** Early rejection of malformed input prevents exploitation deeper in the stack.
- **Consequence:** Unvalidated input is the root cause of injection attacks, buffer overflows, and logic errors.

**SEC-011: Validation MUST Use Allowlists Over Denylists**
- Input validation MUST define what IS allowed (allowlist/whitelist), not what is forbidden (denylist/blacklist).
- **Rationale:** Denylists are always incomplete — new attack vectors bypass them. Allowlists define a closed, verifiable set.
- **Consequence:** Denylist-based validation provides a false sense of security.

```
✅ Correct — Allowlist validation:
RuleFor(x => x.Status)
    .Must(s => AllowedStatuses.Contains(s))
    .WithMessage(ValidationMessages.InvalidStatus);

❌ Incorrect — Denylist validation:
RuleFor(x => x.Status)
    .Must(s => s != "Hacked" && s != "Malicious")
    .WithMessage("Invalid status");
```

**SEC-012: Output MUST Be Encoded to Prevent XSS**
- All dynamic content rendered in HTML, JavaScript, or other output contexts MUST be contextually encoded (HTML-encode, JavaScript-encode, URL-encode).
- **Rationale:** Unencoded output enables Cross-Site Scripting (XSS) attacks.
- **Consequence:** XSS can lead to session hijacking, credential theft, and defacement.

**SEC-013: Validation MUST Cover Type, Range, Length, and Format**
- Input validation MUST enforce: correct data type, acceptable value range (min/max), maximum length, and expected format (regex for emails, phone numbers, etc.).
- **Rationale:** Comprehensive validation prevents overflow, truncation, and type-confusion attacks.
- **Consequence:** Partial validation leaves gaps exploitable by crafted inputs.

### Encryption

**SEC-014: All Communications MUST Use TLS**
- All network communication (API calls, WebSocket connections, external service calls) MUST use HTTPS/TLS. HTTP MUST be redirected to HTTPS.
- **Rationale:** Unencrypted traffic is susceptible to eavesdropping and man-in-the-middle attacks.
- **Consequence:** Plaintext communication exposes credentials, tokens, and sensitive data.

**SEC-015: Passwords MUST Be Hashed with a Salt Using a Strong Algorithm**
- Passwords MUST be hashed using BCrypt, PBKDF2, Argon2, or an equivalent adaptive hashing algorithm with a unique, random salt per password.
- Plain-text passwords MUST NOT be stored, transmitted, or logged under any circumstance.
- **Rationale:** Salted adaptive hashing resists rainbow table attacks and brute-force.
- **Consequence:** Plain-text or weakly hashed passwords result in mass credential compromise upon data breach.

**SEC-016: Sensitive Data at Rest MUST Be Encrypted**
- Data classified as sensitive (payment information, personal identification numbers, health data) MUST be encrypted at the storage layer using AES-256 or equivalent.
- **Rationale:** Encryption at rest protects data if the storage medium is compromised.
- **Consequence:** Unencrypted sensitive data at rest violates most data protection regulations.

### Secret Management

**SEC-017: Secrets MUST NOT Be Hardcoded in Source Code**
- API keys, connection strings, JWT signing keys, OAuth client secrets, and any credentials MUST NOT appear as string literals in source code.
- **Rationale:** Hardcoded secrets are discoverable through source code access, decompilation, or repository leaks.
- **Consequence:** A single leaked repository exposes all hardcoded credentials.

**SEC-018: Secrets MUST NOT Be Committed to Version Control**
- Configuration files containing secrets (e.g., `appsettings.json` with real credentials) MUST be excluded from version control via `.gitignore`. Secret files MUST be managed through environment variables, user secrets (development), or secret vaults (production).
- **Rationale:** Version control history retains secrets permanently even after deletion.
- **Consequence:** Committed secrets require immediate rotation of all exposed credentials.

```
✅ Correct — Secrets externalized:
# appsettings.json (committed) — no real values
{
  "Jwt": {
    "Key": "REPLACE_VIA_ENVIRONMENT",
    "Issuer": "https://api.example.com"
  }
}

# Environment variable or secret vault provides actual value:
JWT__KEY=<actual-signing-key>

❌ Incorrect — Secrets hardcoded:
{
  "Jwt": {
    "Key": "mySuperSecretKey12345!",
    "Issuer": "https://api.example.com"
  },
  "ConnectionStrings": {
    "Default": "Server=prod-db;Password=P@ssw0rd123;"
  }
}
```

**SEC-019: Secrets MUST Be Rotated on a Defined Schedule**
- All secrets (API keys, signing keys, database passwords) MUST have a defined rotation schedule. Automated rotation SHOULD be used where possible.
- **Rationale:** Limits the blast radius of an undetected credential compromise.
- **Consequence:** Stale, never-rotated secrets accumulate risk over time.

### OWASP Top 10

**SEC-020: Database Queries MUST Use Parameterized Statements**
- All database queries MUST use parameterized queries, prepared statements, or an ORM that generates parameterized SQL. String concatenation to build SQL MUST NOT be used.
- **Rationale:** Parameterized queries are the primary defense against SQL Injection.
- **Consequence:** SQL Injection can lead to complete database compromise.

```
✅ Correct — Parameterized query:
var orders = await context.Orders
    .Where(o => o.TableId == tableId && o.Status == status)
    .ToListAsync();

// If raw SQL is needed:
var results = await context.Database
    .SqlInterpolated($"SELECT * FROM Orders WHERE TableId = {tableId}")
    .ToListAsync();

❌ Incorrect — String concatenation:
var sql = "SELECT * FROM Orders WHERE TableId = '" + tableId + "'";
var results = await context.Database.ExecuteSqlRawAsync(sql);
```

**SEC-021: CORS MUST Be Configured with Explicit Allowed Origins**
- CORS policies MUST specify exact allowed origins. Wildcard (`*`) origins MUST NOT be used in any non-development environment.
- **Rationale:** Overly permissive CORS allows malicious sites to make authenticated requests.
- **Consequence:** Wildcard CORS effectively disables same-origin protection.

**SEC-022: Anti-Forgery Tokens MUST Be Used for State-Changing Requests**
- All state-changing operations (`POST`, `PUT`, `PATCH`, `DELETE`) from browser-based clients MUST include anti-forgery tokens (e.g., `X-XSRF-TOKEN` header).
- **Rationale:** When tokens are stored in cookies, browsers auto-attach them, making CSRF attacks possible without anti-forgery validation.
- **Consequence:** Missing CSRF protection allows attackers to perform actions on behalf of authenticated users.

**SEC-023: Rate Limiting MUST Be Applied to Authentication and Public Endpoints**
- Authentication endpoints MUST enforce rate limits (e.g., max 5 attempts per 10 minutes per IP). Public-facing APIs MUST enforce per-client rate limits to prevent abuse and DDoS.
- **Rationale:** Rate limiting is the first line of defense against brute-force and volumetric attacks.
- **Consequence:** Unlimited authentication attempts enable credential stuffing attacks.

**SEC-024: Security Headers MUST Be Set on All HTTP Responses**
- All HTTP responses MUST include at minimum: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Strict-Transport-Security` (HSTS), `Content-Security-Policy`, and `Referrer-Policy`.
- **Rationale:** Security headers instruct browsers to enable built-in protections.
- **Consequence:** Missing headers leave the application vulnerable to clickjacking, MIME-type confusion, and downgrade attacks.

### Sensitive Data Logging

**SEC-025: Sensitive Data MUST NOT Be Written to Logs**
- Passwords, authentication tokens, OTP codes, PINs, credit card numbers, bank account numbers, and any PII MUST NOT appear in application logs (console, file, database, or external logging service).
- **Rationale:** Logs are often stored with less protection than primary data stores, and are frequently shared for debugging.
- **Consequence:** Logged credentials or PII can be harvested from log aggregation systems.

**SEC-026: Sensitive Fields MUST Be Masked in Log Output**
- When logging request/response payloads or error details that may contain sensitive fields, those fields MUST be masked or redacted before writing (e.g., `Password: "******"`, `CardNumber: "4111-XXXX-XXXX-1111"`).
- **Rationale:** Automated logging middleware can inadvertently capture sensitive data.
- **Consequence:** Full payloads in logs expose sensitive data to anyone with log access.

```
✅ Correct — Masked logging:
logger.LogInformation("User login attempt: {UserId}, Password: {Password}",
    request.UserId, "******");

logger.LogInformation("Payment processed: CardNumber={CardNumber}",
    MaskCardNumber(request.CardNumber));  // Output: "4111-XXXX-XXXX-1111"

❌ Incorrect — Raw sensitive data in logs:
logger.LogInformation("User login: {UserId}, Password: {Password}",
    request.UserId, request.Password);

logger.LogDebug("Full request body: {@Request}", request);
// Dumps entire object including tokens, passwords, etc.
```

**SEC-027: Error Responses MUST NOT Expose Internal Details**
- API error responses MUST return generic, externalized error messages (via resource keys). Stack traces, SQL errors, file paths, and internal class names MUST NOT be included in responses to clients.
- **Rationale:** Internal details help attackers understand system internals and craft targeted exploits.
- **Consequence:** Exposed stack traces reveal technology stack, library versions, and internal architecture.

---

## Recommended Practices

**SEC-R01: Multi-Factor Authentication (MFA) SHOULD Be Supported for Privileged Roles**
- Admin and Manager accounts SHOULD support multi-factor authentication as an additional layer of protection.
- **Rationale:** Passwords alone are insufficient for high-privilege accounts.
- **Consequence:** Compromised admin credentials without MFA give attackers full system control.

**SEC-R02: Password Policies SHOULD Enforce Minimum Complexity**
- Systems SHOULD enforce minimum password length (12+ characters), and SHOULD NOT impose arbitrary complexity rules (e.g., must include special character) that encourage weak patterns.
- **Rationale:** Length-based policies are more effective than complexity rules per NIST 800-63B guidelines.
- **Consequence:** Weak password policies increase susceptibility to brute-force attacks.

**SEC-R03: Security-Sensitive Actions SHOULD Be Audit-Logged**
- Login attempts (success and failure), permission changes, data exports, and administrative actions SHOULD be recorded in an immutable audit log.
- **Rationale:** Audit trails enable incident investigation and compliance verification.
- **Consequence:** Without audit logs, security incidents cannot be effectively investigated.

**SEC-R04: Dependency Scanning SHOULD Be Performed Regularly**
- Third-party libraries and packages SHOULD be scanned for known vulnerabilities on a regular schedule (at minimum before each release).
- **Rationale:** Known CVEs in dependencies are a common attack vector.
- **Consequence:** Vulnerable dependencies can be exploited using publicly available exploit code.

**SEC-R05: HTTPS Strict Transport Security SHOULD Use a Long Max-Age**
- HSTS headers SHOULD specify a `max-age` of at least one year (31536000 seconds) and SHOULD include `includeSubDomains`.
- **Rationale:** Short HSTS durations leave windows for downgrade attacks.
- **Consequence:** Users may connect over HTTP during the gap.

---

## Anti-patterns

### AP-01: Security by Obscurity
- **Description:** Relying on hidden URLs, undocumented endpoints, or obscure parameter names as security controls.
- **Why it's harmful:** Attackers use automated scanners that discover hidden endpoints. Obscurity provides zero actual protection.
- **What to do instead:** Apply proper authentication and authorization to every endpoint regardless of discoverability.

### AP-02: Client-Side Only Authorization
- **Description:** Hiding UI elements or disabling buttons as the sole means of restricting access to features.
- **Why it's harmful:** Any user can bypass client-side restrictions using browser dev tools or direct API calls.
- **What to do instead:** Enforce all authorization server-side at the API boundary. Use UI hiding only for improved user experience, never as a security control.

### AP-03: Catch-All Exception Swallowing
- **Description:** Catching all exceptions and returning raw exception messages or stack traces to the client.
- **Why it's harmful:** Exposes internal system details (database structure, file paths, library versions) to potential attackers.
- **What to do instead:** Log the full exception server-side with appropriate severity. Return a generic, externalized error message (via resource key) to the client with a correlation ID for support reference.

### AP-04: Rolling Your Own Cryptography
- **Description:** Implementing custom encryption, hashing, or token generation algorithms instead of using established, audited libraries.
- **Why it's harmful:** Custom cryptography almost always has subtle flaws that are discoverable by skilled attackers.
- **What to do instead:** Use well-established cryptographic libraries (BCrypt, PBKDF2, Argon2 for hashing; AES-256 for encryption; platform-provided JWT libraries for tokens).

### AP-05: Logging Everything for Debugging
- **Description:** Logging complete request/response bodies including headers and payloads at DEBUG level in production.
- **Why it's harmful:** Captures passwords, tokens, PII, and payment data in log stores that may lack proper access controls.
- **What to do instead:** Log only what is necessary. Apply field-level masking. Use structured logging with explicit field selection. Never log at DEBUG level in production.

---

## Examples

### Example 1: Secure Token Configuration

```
✅ Correct — Complete secure cookie configuration:
services.ConfigureAuthCookies(options =>
{
    options.HttpOnly = true;
    options.Secure = true;
    options.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

❌ Incorrect — Insecure token storage:
// Storing JWT in localStorage — vulnerable to XSS
localStorage.setItem("authToken", response.token);

// Fetching with token in URL — leaked in logs and referrer headers
fetch(`/api/orders?token=${authToken}`);
```

### Example 2: Proper Validation Layer

```
✅ Correct — Dedicated validator with allowlist, type, range, and format checks:
public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.TableId)
            .GreaterThan(0)
            .WithMessage(ValidationMessages.TableIdRequired);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage(ValidationMessages.OrderItemsRequired);

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Quantity)
                .InclusiveBetween(1, 100)
                .WithMessage(ValidationMessages.QuantityOutOfRange);

            item.RuleFor(i => i.MenuItemId)
                .GreaterThan(0)
                .WithMessage(ValidationMessages.InvalidMenuItem);
        });
    }
}

❌ Incorrect — Inline validation with hardcoded messages, no dedicated layer:
[HttpPost]
public async Task<IActionResult> CreateOrder(CreateOrderRequest req)
{
    if (req.TableId <= 0)
        return BadRequest("Table ID is invalid");  // hardcoded string
    if (req.Items == null)
        return BadRequest("No items");              // no validation layer
    // Business logic mixed with validation...
}
```

### Example 3: Secret Management Across Environments

```
✅ Correct — Environment-specific secret management:

# Development: Use user secrets
dotnet user-secrets set "Jwt:Key" "dev-only-signing-key"

# Production: Use secret vault or environment variables
# Docker Compose example:
environment:
  - Jwt__Key=${JWT_SIGNING_KEY}       # injected from vault
  - ConnectionStrings__Default=${DB_CONNECTION}

# .gitignore includes:
appsettings.Development.json
appsettings.Production.json

❌ Incorrect — Secrets in committed configuration:
// appsettings.json (committed to git)
{
  "ConnectionStrings": {
    "Default": "Server=192.168.1.100;Database=POS;User=sa;Password=Str0ngP@ss!"
  }
}
```

---

## Checklist

### Authentication
- □ Access tokens have an appropriate expiration time (≤ 2 hours)
- □ Refresh tokens are single-use and rotated on each exchange
- □ Tokens are stored in HttpOnly, Secure, SameSite=Strict cookies (browser apps)
- □ Login error messages are generic and externalized — no user enumeration
- □ Logout invalidates all associated tokens server-side

### Authorization
- □ Every API endpoint declares required roles/permissions
- □ Default access policy is deny (authentication required)
- □ Resource-level authorization verifies ownership/access per request
- □ No authorization logic resides solely in the presentation/UI layer

### Input Validation
- □ All input is validated at the API boundary using a dedicated validation layer
- □ Validation uses allowlist approach (defines what IS allowed)
- □ Type, range, length, and format checks are applied to all fields
- □ All output is contextually encoded to prevent XSS
- □ All user-facing validation messages are externalized (no hardcoded strings)

### Encryption & Passwords
- □ All communications use HTTPS/TLS — HTTP is redirected
- □ Passwords are hashed with BCrypt, PBKDF2, or Argon2 with unique salts
- □ No plain-text passwords exist anywhere (storage, transit, logs)
- □ Sensitive data at rest is encrypted with AES-256 or equivalent

### Secret Management
- □ No secrets exist as string literals in source code
- □ `.gitignore` excludes all files that may contain secrets
- □ Environment variables or secret vaults are used for all credentials
- □ A secret rotation schedule is defined and followed

### OWASP Protections
- □ All database queries use parameterized statements or ORM-generated SQL
- □ CORS is configured with explicit allowed origins (no wildcard in production)
- □ Anti-forgery tokens are required for all state-changing requests
- □ Rate limiting is applied to authentication and public endpoints
- □ Security headers are present on all HTTP responses

### Logging & Error Handling
- □ Passwords, tokens, OTPs, PINs, card numbers, and PII are never logged
- □ Sensitive fields are masked/redacted before writing to logs
- □ Error responses return generic messages with correlation IDs, not stack traces
- □ Audit logging captures security-sensitive actions (login, permission changes)
