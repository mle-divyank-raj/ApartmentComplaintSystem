# Product Requirements

**Document:** `docs/01_Product/product_requirements.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> This document consolidates all functional and non-functional requirements from the project's source documents. These requirements are the acceptance criteria for the system. Every feature implemented must trace to at least one requirement here.

---

## Functional Requirements

### Resident

| ID | Requirement |
|---|---|
| FR-01 | Residents shall be able to register an account using email or phone number via a secure invitation token linked to a unit |
| FR-02 | Residents shall be able to log in and log out securely |
| FR-03 | The system shall authenticate users based on assigned roles (Resident, Manager, MaintenanceStaff) |
| FR-04 | Residents shall be able to submit a new complaint with category, description, and priority |
| FR-05 | Residents shall be able to upload images as complaint evidence |
| FR-06 | The system shall generate a unique complaint ID for each submission |
| FR-07 | Residents shall be able to view the status of their submitted complaints |
| FR-08 | Residents shall be able to trigger an emergency SOS alert to immediately notify all on-call maintenance staff |
| FR-09 | Residents shall be able to view past maintenance tickets including resolution details (resolving staff member and TAT) |
| FR-10 | Residents shall be able to submit a rating (1–5 stars) and feedback comment upon complaint resolution |
| FR-11 | The system shall notify residents of property-wide issues through broadcast notifications sent via email and SMS |

### Administrators (Property Managers)

| ID | Requirement |
|---|---|
| FR-12 | The system shall notify residents when complaint status changes |
| FR-13 | Administrators shall be able to view all complaints across the system |
| FR-14 | Administrators shall be able to assign complaints to maintenance staff |
| FR-15 | Administrators shall be able to prioritize complaints based on severity |
| FR-16 | Administrators shall be able to view real-time dashboards displaying all open and in-progress complaints along with current task assignments |
| FR-17 | Administrators shall be able to view historical complaints grouped by maintenance staff, including tasks resolved and average resident rating |
| FR-18 | Administrators shall be able to view historical complaints grouped by apartment units to identify recurring issues |
| FR-19 | Administrators shall be able to create and declare outage events by specifying outage type, time window, and description |
| FR-20 | The system shall trigger mass notifications to all residents when an outage event is declared |
| FR-21 | Administrators shall be able to reassign complaints if needed |

### Maintenance Staff

| ID | Requirement |
|---|---|
| FR-22 | Maintenance staff shall be able to view only their assigned complaints |
| FR-23 | Maintenance staff shall be able to update complaint status (In Progress, Resolved) |
| FR-24 | Maintenance staff shall be able to add work notes to complaints |
| FR-25 | Maintenance staff shall be able to upload completion photos |
| FR-26 | Maintenance staff shall be able to update the estimated completion time for an active ticket |
| FR-27 | The system shall notify the resident when the estimated completion time is updated |
| FR-28 | Maintenance staff shall be able to update their availability status (Available, Busy, On Break) |

### System-Driven

| ID | Requirement |
|---|---|
| FR-29 | The system shall log timestamps for complaint creation, updates, and resolution |
| FR-30 | The system shall maintain a history of all complaint status changes |
| FR-31 | The system shall allow administrators to generate complaint summary reports |
| FR-32 | The system shall allow administrators to search for complaints by category, status, and date |
| FR-33 | The system shall allow administrators to filter complaints by priority |
| FR-34 | The system shall display dashboards showing open, in-progress, and closed complaints |
| FR-35 | The system shall allow residents to receive notifications via email or in-app alerts |
| FR-36 | The system shall restrict users from accessing unauthorized complaints |
| FR-37 | The system shall allow administrators to manage user accounts |
| FR-38 | The system shall allow administrators to deactivate or reactivate user accounts |
| FR-39 | The system shall support emergency or high-priority complaint tagging |
| FR-40 | The system shall allow residents to provide feedback after complaint resolution |
| FR-41 | The system shall archive closed complaints for future reference |

---

## Non-Functional Requirements

| ID | Requirement |
|---|---|
| NFR-01 | The system shall be available at least 99% of the time |
| NFR-02 | The system shall load core pages within 2 seconds on standard broadband or 4G networks |
| NFR-03 | The system shall support at least 5,000 apartment units without performance degradation |
| NFR-04 | The system shall encrypt all data in transit using HTTPS/TLS |
| NFR-05 | The system shall securely hash and store passwords using industry-standard algorithms |
| NFR-06 | The system shall ensure data isolation between different apartment properties |
| NFR-07 | The system shall be responsive and usable on mobile and desktop devices |
| NFR-08 | The system shall comply with WCAG 2.1 Level AA accessibility guidelines |
| NFR-09 | The system shall perform automated daily database backups |
| NFR-10 | The system shall maintain an immutable audit trail for all critical actions |
| NFR-11 | The system shall be designed using a modular, scalable architecture |
| NFR-12 | The notification engine shall send at least 500 SMS/email messages within 60 seconds for a property-wide outage event without database locking or performance degradation |
| NFR-13 | Resident feedback and ratings shall be visible only to property managers and optionally the resolving staff member — not to other residents |
| NFR-14 | TAT and average maintenance staff ratings shall be calculated asynchronously or using optimised indexing to prevent dashboard performance degradation |

---

*End of Product Requirements v1.0*
