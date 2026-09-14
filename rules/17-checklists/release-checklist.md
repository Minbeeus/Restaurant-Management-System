# Release Checklist

> **Rule Hierarchy Level:** 17
> **Applies To:** Release Managers and DevOps Engineers
> **Last Updated:** 2026-07-14

This checklist MUST be executed and signed off prior to launching a release build to the production environment.

---

## 1. Pre-Release Verification
- [ ] **Feature Freeze Met:** All code changes intended for the release version are merged, and the build branch is locked.
- [ ] **API Documentation Synchronized:** Swagger/OpenAPI schemas and public documentation catalogs match the current release contract.
- [ ] **Changelog Compiled:** The `CHANGELOG.md` file contains a detailed list of additions, fixes, changes, deprecations, and security upgrades for this version.
- [ ] **Semantic Versioning:** The release version conforms to `vMAJOR.MINOR.PATCH` and release tags are created in Git.

---

## 2. Database Migration Readiness
- [ ] **SQL Generation Verification:** Run database migration scripts against a staging database clone and inspect the generated SQL for performance issues or accidental drops.
- [ ] **Rollback Plan Tested:** Executed and verified database migration rollback scripts successfully on the staging database.
- [ ] **Backup Executed:** Completed a backup of the production database prior to starting the release deployment.

---

## 3. CI/CD Pipeline Gates
- [ ] **Automated Pipeline Green:** The build runner has successfully compiled the package and run all tests with zero errors.
- [ ] **Security Vulnerability Scan:** The security scanner reports zero critical or high vulnerabilities in third-party NuGet, NPM, or base container packages.
- [ ] **Static Code Analysis Check:** Static analysis scores meet or exceed project quality gate requirements.

---

## 4. Deployment & Infrastructure Configuration
- [ ] **Environment Configuration Verification:** Double-checked that all production connection strings, API tokens, and secret vault bindings are configured and active.
- [ ] **Readiness Probe Validation:** Verified that `/health/ready` check configs will not trigger premature routing to updating containers.
- [ ] **Rollback Configuration Enabled:** Container orchestrators are configured to automatically roll back to the previous tag if startup health checks fail.

---

## 5. Post-Deployment Verification
- [ ] **Health Dashboard Check:** Monitored liveness and readiness statuses across all container nodes.
- [ ] **Smoke Testing:** Completed validation of critical user paths (e.g., ordering, payment checkout, user logins) in the live production environment.
- [ ] **Log Monitoring:** Inspected Seq/Serilog dashboards for an uptick in uncaught exception logs.
