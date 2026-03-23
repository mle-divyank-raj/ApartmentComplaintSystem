# Data Classification Policy

**Document:** `docs/10_Security/data_classification_policy.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

## 1. Classification Tiers

| Tier | Label | Definition |
|---|---|---|
| 1 | **Confidential** | Personal data or credentials. Exposure causes direct harm. Strict access controls required. |
| 2 | **Internal** | Operational data visible to authenticated users within their property scope. Not public. |
| 3 | **Public** | Data that may be shared with any party without restriction. ACLS has no Tier 3 data in V1. |

---

## 2. Data Classification by Entity

### Users Table

| Field | Tier | Notes |
|---|---|---|
| `PasswordHash` | Confidential | Never returned in API responses. Never logged. |
| `Email` | Confidential | PII — returned only to the owning user and their Manager. |
| `Phone` | Confidential | PII — used for SMS only, never displayed in the Manager Dashboard. |
| `FirstName`, `LastName` | Internal | Shown to Manager and assigned Staff. |
| `Role`, `IsActive` | Internal | Operational metadata. |

### Complaints Table

| Field | Tier | Notes |
|---|---|---|
| `ResidentFeedbackComment` | Confidential | Visible to Manager and resolving Staff only (NFR-13). Never to other Residents. |
| `ResidentRating` | Confidential | Same visibility restriction as feedback comment. |
| `Description` | Internal | Visible to Manager and assigned Staff. Resident sees own complaints only. |
| `Title`, `Category`, `Status` | Internal | Operational — scoped to property. |

### Media Table

| Field | Tier | Notes |
|---|---|---|
| `Url` | Internal | Blob URL. Access controlled at API layer. Not publicly browsable. |

### AuditLog Table

| Field | Tier | Notes |
|---|---|---|
| `OldValue`, `NewValue` | Confidential | May contain PII snapshots. Never exposed via API. Internal system use only. |
| `ActorUserId`, `IpAddress` | Confidential | PII. Internal system use only. |
| `Action`, `EntityType`, `EntityId` | Internal | Operational metadata. |

---

## 3. Data Handling Rules

| Rule | Detail |
|---|---|
| **Confidential data in logs** | Never. See `docs/07_Implementation/observability.md` Section 4.4. |
| **Confidential data in API responses** | Only to the owning user or their authorised Manager, within the same property scope. |
| **Feedback visibility** | `ResidentRating` and `ResidentFeedbackComment` must be excluded from all API responses when the requester has the `Resident` role. Included only for `Manager` and the specific `MaintenanceStaff` who resolved the complaint. |
| **Cross-property data** | Strictly prohibited. Repository-level `PropertyId` filter is the primary control. |
| **Data retention** | Complaints are archived (status = CLOSED) and retained indefinitely in V1. No deletion. A formal retention policy is a V2 concern. |
| **Backup encryption** | Azure SQL automated backups are encrypted at rest by Azure. No additional configuration required. |

---

## 4. GDPR Considerations (V1 Scope Note)

ACLS stores personal data (names, emails, phone numbers) for EU residents if deployed in an EU region. A full GDPR compliance review is recommended before EU production deployment. V1 does not implement:
- Right to erasure (delete account and data)
- Data export (download my data)
- Consent management for notifications

These are V2 items. The Data Classification Policy documents what data exists so that a GDPR assessment can be performed when the time comes.

---

*End of Data Classification Policy v1.0*
