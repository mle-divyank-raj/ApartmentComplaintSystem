# SLO Definitions

**Document:** `docs/09_Operations/slo_definitions.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

## Service Level Objectives

These SLOs are derived directly from the project's Non-Functional Requirements (NFR-01 through NFR-14). They define the measurable targets the system must meet in production.

| SLO | Target | Measurement window | NFR |
|---|---|---|---|
| **Availability** | ≥ 99% uptime | Rolling 30 days | NFR-01 |
| **API response time (p95)** | ≤ 2 seconds for all GET endpoints | Rolling 24 hours | NFR-02 |
| **Outage broadcast throughput** | ≥ 500 SMS/email messages within 60 seconds | Per outage declaration event | NFR-12 |
| **Scale** | No performance degradation at 5,000 active units | Load test baseline | NFR-03 |
| **Data backup** | Automated daily backup with ≤ 24-hour RPO | Daily schedule | NFR-09 |

---

## Measurement Sources

| SLO | Measured via |
|---|---|
| Availability | Azure Monitor availability alert on `/healthz` endpoint |
| API response time | Application Insights — request duration p95 per endpoint |
| Broadcast throughput | `Outages.NotificationSentAt - Outages.DeclaredAt` stored in DB, queried by Worker |
| Scale | k6 load test in `tests/performance/k6/` run against staging |

---

## Error Budget

Based on 99% availability over 30 days: **432 minutes of allowable downtime per month**.

Unplanned downtime beyond 432 minutes in a 30-day window triggers a post-incident review.

---

*End of SLO Definitions v1.0*
