# Security Model

**Document:** `docs/03_Architecture/security_model.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

## 1. Authentication

**Mechanism:** JWT Bearer tokens, self-issued by `ACLS.Api`.  
**Algorithm:** HMAC-SHA256  
**Signing key:** `ACLS_JWT_SECRET` environment variable (min 32 characters). Never committed to source control.  
**Token lifetime:** 60 minutes. No refresh tokens in V1.  
**Claims:** `sub` (UserId), `email`, `role`, `property_id`, `exp`, `iss`, `aud`

**Token validation on every authenticated request:**
1. Signature verification (HMAC-SHA256 with `ACLS_JWT_SECRET`)
2. Expiry check (`exp` claim)
3. Issuer check (`iss = "acls-api"`)
4. Audience check (`aud = "acls-clients"`)
5. `TenancyMiddleware` extracts and validates `property_id` claim

---

## 2. Authorisation

**Mechanism:** Role-based access control (RBAC) via `[Authorize(Roles = "...")]` attributes on controllers.

**Roles and permissions summary:**

| Role | What they can access |
|---|---|
| `Resident` | Own complaints only. Own feedback. Outages for their property. |
| `Manager` | All data within their `PropertyId`. Full complaint management, reports, user management. |
| `MaintenanceStaff` | Only their own assigned complaints. Their own profile and availability. |

**Cross-property access returns 404** — not 403. A Resident requesting another property's complaint ID receives `404 Not Found`. See `docs/07_Implementation/patterns/multi_tenancy_pattern.md` Section 7.

---

## 3. Transport Security

- All production traffic: HTTPS (TLS 1.2 minimum), enforced by Azure App Service
- `app.UseHttpsRedirection()` in the middleware pipeline
- `Strict-Transport-Security` header applied by Azure Front Door / App Service
- Local development: HTTP on port 5000 only

---

## 4. Password Security

- BCrypt with minimum work factor 12
- Passwords never stored, logged, or transmitted after initial login
- `PasswordHash` column never returned in any API response or log entry

---

## 5. Secret Management

All secrets managed via Azure Key Vault in staging and production. Never committed to source control. Full reference: `docs/09_Operations/environment_config.md`.

---

## 6. Multi-Tenancy Security

- `PropertyId` is the tenancy discriminator on every property-scoped entity
- `PropertyId` is always sourced from the authenticated JWT claim — never from client-supplied request data
- `TenancyMiddleware` enforces this on every authenticated request
- Every repository query applies `WHERE PropertyId = @propertyId` as the first filter
- A query without a `PropertyId` filter is a security violation

---

## 7. Media Security

- Media files are stored in Azure Blob Storage — not in MSSQL
- Access to media URLs is controlled at the API layer — clients receive URLs via `ComplaintDto.media[].url`
- Direct blob URL access without API authentication is not permitted in V1
- MIME type and file size validation enforced in the controller before upload

---

## 8. Audit Trail

All critical actions are recorded in the `AuditLog` table:
- `AuditEntry` rows are immutable — no update or delete operations permitted
- `IAuditRepository` exposes `AddAsync` only
- Audit entries include: action type, entity ID, actor user ID, actor role, property ID, old/new values (JSON snapshot), timestamp, IP address

---

*End of Security Model v1.0*
