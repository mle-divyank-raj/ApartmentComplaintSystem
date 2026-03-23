# Glossary

**Document:** `docs/00_Project_Constitution/glossary.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This glossary defines project-specific terms used in documents, meetings, and code reviews. For canonical code-level term definitions (class names, banned synonyms), see `docs/02_Domain/ubiquitous_language.md`. This glossary covers broader project and process vocabulary.

---

| Term | Definition |
|---|---|
| **ACLS** | Apartment Complaint Logging System — the abbreviation for this project |
| **Actor** | A user type that interacts with the system: Resident, Manager, or MaintenanceStaff |
| **Aggregate** | A cluster of domain entities treated as a single unit for data changes. `Complaint` is the primary aggregate in ACLS. |
| **Antigravity Claude** | The AI coding assistant used to generate production code for this project. Must always read the mandatory documents before generating any output. |
| **AuditLog** | The immutable record of every critical action taken in the system. Rows are never modified or deleted. |
| **Blast** | The act of sending a simultaneous notification to all members of a group (e.g. all on-call staff during SOS, all residents during an outage). Must be concurrent, not sequential. |
| **Bounded Context** | A domain area with a clearly defined boundary, its own vocabulary, and its own set of entities. ACLS has eight bounded contexts. |
| **Clean Architecture** | The layered backend structure used in ACLS: SharedKernel → Domain → Application → Infrastructure/Persistence → Api. Dependencies point inward only. |
| **DDD** | Domain-Driven Design — the approach used to structure the ACLS backend. |
| **Domain Event** | An immutable record that something significant happened in the domain. Published after a state change is committed. Consumed by other contexts asynchronously. |
| **ETA** | Estimated Time of Completion — the datetime by which staff expects to resolve a complaint. Set by staff, not the system. |
| **FR** | Functional Requirement — a feature the system must provide. FR-01 through FR-41 in this project. |
| **Invitation Token** | A secure, single-use, time-limited token sent to a prospective Resident to enable self-registration. |
| **MediatR** | The .NET library used to implement the command/query pattern in `ACLS.Application`. |
| **Multi-tenancy** | The architectural property that allows multiple independent properties to share the same system while keeping their data completely isolated. Enforced via `PropertyId`. |
| **NFR** | Non-Functional Requirement — a quality attribute the system must have. NFR-01 through NFR-14 in this project. |
| **Phase** | A development stage. ACLS has 7 phases (Domain, Business Logic, API, Frontends, Worker, Tests, Infrastructure). |
| **PropertyId** | The integer identifier of a Property. The tenancy discriminator — present as a column on every property-scoped database table and as a claim in every authenticated JWT. |
| **Smart Dispatch** | The algorithmic service that scores and ranks available staff for a given complaint. Returns a ranked list — never makes assignments directly. |
| **SOS** | Save Our Souls — the emergency protocol triggered by a Resident for life/property-threatening situations. Bypasses normal triage and immediately notifies all on-call staff simultaneously. |
| **TAT** | Turn-Around Time — the elapsed time between complaint submission and resolution. Calculated asynchronously by `ACLS.Worker`. |
| **TenancyMiddleware** | The ASP.NET Core middleware that reads `property_id` from the JWT and populates `ICurrentPropertyContext` for the request duration. |
| **TestContainers** | The .NET library used to spin up a real Docker SQL Server container for integration tests. |
| **Thin Frontend** | The principle that client apps (React, Kotlin, Swift) contain zero business logic — only rendering and API calls. |
| **Worker** | `ACLS.Worker` — the background .NET service that handles async tasks: TAT calculation, average rating updates, and notification fan-out. |

---

*End of Glossary v1.0*
