# Project Charter

**Document:** `docs/00_Project_Constitution/project_charter.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)  
**Client:** Apartment Complex Association  
**Vendor:** UTD CS 3354  
**Period:** January 30, 2026 – May 6, 2026

---

## 1. Mission

Build a web-based, multi-tenant SaaS platform that digitises the full request-to-resolution lifecycle for apartment maintenance complaints — replacing fragmented phone calls, emails, and paper logs with a single system connecting Residents, Property Managers, and Maintenance Staff.

## 2. Vision

A property where every maintenance issue is tracked, assigned, and resolved transparently — where residents always know the status of their complaint, managers always know the workload of their team, and staff never lose track of an assignment.

## 3. Success Criteria

| Criterion | Measure |
|---|---|
| All 41 functional requirements implemented | FR-01 through FR-41 per `docs/01_Product/product_requirements.md` |
| All 14 non-functional requirements met | NFR-01 through NFR-14 per `docs/01_Product/product_requirements.md` |
| System deployed and accessible | Live URL with verified uptime |
| All user roles functional | Resident, Manager, MaintenanceStaff flows tested end-to-end |
| Smart Dispatch algorithm working | Ranked recommendations returned for assignment |
| SOS emergency flow functional | All on-call staff notified within 10 seconds |
| Outage broadcast functional | 500 notifications delivered within 60 seconds (NFR-12) |

## 4. Scope

**In scope:** Everything described in `docs/00_Project_Constitution/repository_blueprint.md` and the Statement of Work.

**Out of scope (V1):** Push notifications (FCM/APNs), SignalR real-time dashboard, refresh tokens, rate limiting, resident web portal, service marketplace. Full list in `docs/03_Architecture/system_overview.md` Section 11.

## 5. Key Stakeholders

| Role | Responsibility |
|---|---|
| Apartment Complex Association | Client — defines requirements, approves deliverables |
| UTD CS 3354 | Vendor — designs, builds, and delivers the system |
| Professor Srinivasan | Academic supervisor |

## 6. Governing Documents

This charter is subordinate to the Statement of Work (`SOW_-_Apartment_Complaint_Logging_System.docx`). In case of conflict, the SOW prevails for contractual matters. For technical decisions, `docs/00_Project_Constitution/repository_blueprint.md` and `docs/00_Project_Constitution/ai_collaboration_rules.md` prevail.

---

*End of Project Charter v1.0*
