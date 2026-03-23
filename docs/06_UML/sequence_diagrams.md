# Sequence Diagrams — ACLS Critical Workflows

**Document:** `docs/06_UML/sequence_diagrams.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> These sequence diagrams are the authoritative specification for the five most critical workflows in ACLS. Every step, every actor, every message, and every ordering shown here must be reflected exactly in the implementation. If the code diverges from a diagram, the code is wrong — not the diagram. Read the relevant diagram before implementing any of these workflows.

---

## Diagram Index

| # | Workflow | Triggered by | Key constraint |
|---|---|---|---|
| 1 | Submit Complaint (with media) | Resident | Blob upload before DB write |
| 2 | Assign Complaint | Manager | Atomic transaction — complaint + staff in one commit |
| 3 | Resolve Complaint | Maintenance Staff | Atomic transaction + async notification |
| 4 | SOS Emergency Trigger | Resident | Simultaneous blast to all on-call staff |
| 5 | Declare Outage | Manager | Async broadcast — must not block HTTP response |

---

## Diagram 1 — Submit Complaint (with Media)

**Trigger:** Resident submits a new complaint via `POST /api/v1/complaints` with multipart/form-data including up to 3 photo files.

**Key constraint:** Blob storage upload happens in the controller, before the command is dispatched. Binary content never reaches the Application or Domain layers. Only URL strings are passed in the command.

```mermaid
sequenceDiagram
    actor Resident
    participant ResidentApp as ResidentApp<br/>(Android/iOS)
    participant API as ACLS.Api<br/>ComplaintsController
    participant TenancyMW as TenancyMiddleware
    participant Storage as IStorageService<br/>(AzureBlobStorageService)
    participant Mediator as MediatR
    participant Handler as SubmitComplaintCommandHandler
    participant DB as AclsDbContext<br/>(MSSQL)
    participant Publisher as IPublisher
    participant Worker as ACLS.Worker

    Resident->>ResidentApp: Fills complaint form, selects photos
    ResidentApp->>API: POST /api/v1/complaints<br/>(multipart/form-data: fields + files)

    API->>TenancyMW: Extract property_id from JWT
    TenancyMW-->>API: ICurrentPropertyContext populated<br/>(PropertyId, UserId)

    Note over API: Step 1 — Upload files to blob storage<br/>(before command is built)

    loop For each media file (max 3)
        API->>Storage: UploadAsync(stream, fileName, contentType)
        Storage-->>API: blobUrl (string)
    end

    Note over API: Step 2 — Build command with URL strings only<br/>(no streams, no binary)

    API->>Mediator: Send(SubmitComplaintCommand)<br/>MediaFiles = [MediaUploadResult(url, type)]

    Note over Mediator: ValidationBehaviour runs<br/>SubmitComplaintValidator

    Mediator->>Handler: Handle(command, ct)

    Handler->>Handler: Complaint.Create(...)<br/>sets Status=OPEN, CreatedAt=UtcNow
    Handler->>Handler: Media.Create(url, type) per file<br/>URL string only — no binary

    Handler->>DB: AddAsync(complaint + media)
    DB-->>Handler: Saved (ComplaintId assigned)

    Handler->>Publisher: Publish(ComplaintSubmittedEvent)
    Publisher-->>Handler: Event queued

    Handler-->>Mediator: Result.Success(ComplaintDto)
    Mediator-->>API: ComplaintDto

    API-->>ResidentApp: 201 Created<br/>ComplaintDto (with media[].url)
    ResidentApp-->>Resident: "Complaint submitted"

    Note over Worker: Async — does not block response
    Worker->>Worker: ComplaintSubmittedEventHandler<br/>Writes AuditEntry
```

---

## Diagram 2 — Assign Complaint

**Trigger:** Manager assigns a complaint to a staff member via `POST /api/v1/complaints/{id}/assign`.

**Key constraint:** The complaint status change (`OPEN → ASSIGNED`) and the staff availability change (`AVAILABLE → BUSY`) must be committed in a single database transaction. If either write fails, both are rolled back. The system must never be in a state where a complaint is ASSIGNED but the staff member is still AVAILABLE.

```mermaid
sequenceDiagram
    actor Manager
    participant Dashboard as ResidentApp.Web<br/>(Manager Dashboard)
    participant API as ACLS.Api<br/>ComplaintsController
    participant TenancyMW as TenancyMiddleware
    participant Mediator as MediatR
    participant TxBehaviour as TransactionBehaviour<br/>(MediatR Pipeline)
    participant Handler as AssignComplaintCommandHandler
    participant ComplaintRepo as IComplaintRepository
    participant StaffRepo as IStaffRepository
    participant DB as AclsDbContext<br/>(MSSQL)
    participant Publisher as IPublisher
    participant Worker as ACLS.Worker
    participant NotifSvc as INotificationService

    Manager->>Dashboard: Selects complaint, selects staff from ranked list, clicks Assign
    Dashboard->>API: POST /api/v1/complaints/{id}/assign<br/>Body: { staffMemberId: 5 }

    API->>TenancyMW: Extract property_id from JWT
    TenancyMW-->>API: ICurrentPropertyContext (PropertyId)

    API->>Mediator: Send(AssignComplaintCommand)

    Note over Mediator: ValidationBehaviour — checks staffMemberId present

    Mediator->>TxBehaviour: Begin transaction
    TxBehaviour->>Handler: Handle(command, ct)

    Handler->>ComplaintRepo: GetByIdAsync(complaintId, propertyId)
    ComplaintRepo->>DB: SELECT WHERE ComplaintId=X AND PropertyId=Y
    DB-->>ComplaintRepo: Complaint (or null)
    ComplaintRepo-->>Handler: complaint

    alt complaint is null
        Handler-->>TxBehaviour: Result.Failure(ComplaintErrors.NotFound)
        TxBehaviour-->>API: Rollback (nothing written)
        API-->>Dashboard: 404 Not Found
    end

    Handler->>StaffRepo: GetByIdAsync(staffMemberId, propertyId)
    StaffRepo->>DB: SELECT WHERE StaffMemberId=X AND PropertyId=Y
    DB-->>StaffRepo: StaffMember (or null)
    StaffRepo-->>Handler: staff

    alt staff is null
        Handler-->>TxBehaviour: Result.Failure(StaffErrors.NotFound)
        TxBehaviour-->>API: Rollback (nothing written)
        API-->>Dashboard: 404 Not Found
    end

    Note over Handler: Both entities loaded — begin mutations

    Handler->>Handler: complaint.Assign(staffMemberId)<br/>→ Status=ASSIGNED, raises ComplaintAssignedEvent
    Handler->>Handler: staff.MarkBusy()<br/>→ Availability=BUSY, LastAssignedAt=UtcNow

    Handler->>ComplaintRepo: UpdateAsync(complaint)
    ComplaintRepo->>DB: UPDATE Complaints SET status='ASSIGNED'...
    Handler->>StaffRepo: UpdateAsync(staff)
    StaffRepo->>DB: UPDATE StaffMembers SET availability='BUSY'...

    Note over TxBehaviour: COMMIT — both writes atomic
    TxBehaviour->>DB: COMMIT TRANSACTION
    DB-->>TxBehaviour: Committed

    Handler->>Publisher: Publish(ComplaintAssignedEvent)
    Publisher-->>Handler: Queued

    Handler-->>TxBehaviour: Result.Success(ComplaintDto)
    TxBehaviour-->>Mediator: Result.Success(ComplaintDto)
    Mediator-->>API: ComplaintDto

    API-->>Dashboard: 200 OK — ComplaintDto (status: ASSIGNED)
    Dashboard-->>Manager: Complaint shown as ASSIGNED

    Note over Worker: Async — after HTTP response returned
    Worker->>NotifSvc: NotifyStaff(staffMemberId, complaint)
    NotifSvc-->>Worker: SMS/Email sent to staff
    Worker->>DB: WriteAuditEntry(ComplaintAssigned)
```

---

## Diagram 3 — Resolve Complaint

**Trigger:** Maintenance Staff marks a complaint resolved via `POST /api/v1/complaints/{id}/resolve` with optional completion photos.

**Key constraint:** The complaint status (`→ RESOLVED`) and staff availability (`→ AVAILABLE`) are committed atomically. Resident notification happens asynchronously after the commit — it must not block the HTTP response. TAT calculation is also async.

```mermaid
sequenceDiagram
    actor Staff
    participant StaffApp as StaffApp<br/>(Android/iOS)
    participant API as ACLS.Api<br/>ComplaintsController
    participant TenancyMW as TenancyMiddleware
    participant Storage as IStorageService
    participant Mediator as MediatR
    participant TxBehaviour as TransactionBehaviour
    participant Handler as ResolveComplaintCommandHandler
    participant ComplaintRepo as IComplaintRepository
    participant StaffRepo as IStaffRepository
    participant DB as AclsDbContext<br/>(MSSQL)
    participant Publisher as IPublisher
    participant Worker as ACLS.Worker
    participant NotifSvc as INotificationService
    actor Resident

    Staff->>StaffApp: Taps "Mark Resolved", adds resolution notes,<br/>optionally attaches completion photos
    StaffApp->>API: POST /api/v1/complaints/{id}/resolve<br/>(multipart/form-data)

    API->>TenancyMW: Extract property_id from JWT
    TenancyMW-->>API: ICurrentPropertyContext (PropertyId, UserId)

    Note over API: Upload completion photos first (same pattern as submit)
    opt Completion photos attached
        loop For each photo (max 3)
            API->>Storage: UploadAsync(stream, fileName, contentType)
            Storage-->>API: blobUrl (string)
        end
    end

    API->>Mediator: Send(ResolveComplaintCommand)<br/>CompletionPhotos=[MediaUploadResult(url,type)]

    Mediator->>TxBehaviour: Begin transaction
    TxBehaviour->>Handler: Handle(command, ct)

    Handler->>ComplaintRepo: GetByIdAsync(complaintId, propertyId)
    DB-->>Handler: complaint

    alt complaint.Status != IN_PROGRESS
        Handler-->>TxBehaviour: Result.Failure(InvalidStatusTransition)
        TxBehaviour-->>API: Rollback
        API-->>StaffApp: 422 Unprocessable Entity
    end

    Handler->>StaffRepo: GetByIdAsync(staff.StaffMemberId, propertyId)
    DB-->>Handler: staff

    Note over Handler: Mutate both entities

    Handler->>Handler: complaint.Resolve(resolutionNotes)<br/>→ Status=RESOLVED, ResolvedAt=UtcNow<br/>→ raises ComplaintResolvedEvent

    Handler->>Handler: staff.MarkAvailable()<br/>→ Availability=AVAILABLE

    opt Completion photos present
        Handler->>Handler: complaint.AddMedia(url, type, uploadedByUserId)<br/>per completion photo URL
    end

    Handler->>ComplaintRepo: UpdateAsync(complaint)
    DB->>DB: UPDATE Complaints SET status='RESOLVED', resolved_at=...
    Handler->>StaffRepo: UpdateAsync(staff)
    DB->>DB: UPDATE StaffMembers SET availability='AVAILABLE'

    Note over TxBehaviour: COMMIT — complaint + staff atomic
    TxBehaviour->>DB: COMMIT TRANSACTION

    Handler->>Publisher: Publish(ComplaintResolvedEvent)

    Handler-->>API: Result.Success(ComplaintDto)
    API-->>StaffApp: 200 OK — ComplaintDto (status: RESOLVED)
    StaffApp-->>Staff: "Complaint marked resolved"

    Note over Worker: Async — resident notified after HTTP response
    Worker->>NotifSvc: NotifyResident(residentId, complaint)
    NotifSvc-->>Resident: "Your complaint has been resolved" (SMS/Email/Push)

    Worker->>Worker: CalculateTatJob<br/>tat = ResolvedAt - CreatedAt (minutes)
    Worker->>DB: UPDATE Complaints SET tat=X WHERE ComplaintId=Y

    Worker->>DB: WriteAuditEntry(ComplaintResolved)
```

---

## Diagram 4 — SOS Emergency Trigger

**Trigger:** Resident activates the SOS panic button via `POST /api/v1/complaints/sos`.

**Key constraint:** The SOS flow bypasses normal triage. All available on-call staff are notified simultaneously — not one-at-a-time, not waiting for manager action. The notification blast is concurrent. The complaint is immediately moved to ASSIGNED without manager intervention.

```mermaid
sequenceDiagram
    actor Resident
    participant ResidentApp as ResidentApp<br/>(Android/iOS)
    participant API as ACLS.Api<br/>ComplaintsController
    participant TenancyMW as TenancyMiddleware
    participant Mediator as MediatR
    participant TxBehaviour as TransactionBehaviour
    participant Handler as TriggerSosCommandHandler
    participant ComplaintRepo as IComplaintRepository
    participant DB as AclsDbContext<br/>(MSSQL)
    participant Publisher as IPublisher
    participant Worker as ACLS.Worker
    participant DispatchSvc as IDispatchService
    participant NotifSvc as INotificationService
    participant Staff1 as Staff Member 1
    participant Staff2 as Staff Member 2
    participant StaffN as Staff Member N

    Resident->>ResidentApp: Presses SOS panic button
    ResidentApp->>ResidentApp: Confirms emergency (prevent accidental trigger)
    ResidentApp->>API: POST /api/v1/complaints/sos<br/>Body: { title, description, permissionToEnter }

    API->>TenancyMW: Extract property_id from JWT
    TenancyMW-->>API: ICurrentPropertyContext (PropertyId, UserId)

    API->>Mediator: Send(TriggerSosCommand)

    Mediator->>TxBehaviour: Begin transaction
    TxBehaviour->>Handler: Handle(command, ct)

    Handler->>Handler: Complaint.Create(...)<br/>urgency=SOS_EMERGENCY<br/>status=OPEN initially

    Handler->>Handler: complaint.MarkAsEmergency()<br/>→ status=ASSIGNED (skips OPEN)<br/>→ raises SosTriggeredEvent

    Handler->>ComplaintRepo: AddAsync(complaint)
    DB-->>Handler: Saved (ComplaintId assigned)

    TxBehaviour->>DB: COMMIT TRANSACTION

    Handler->>Publisher: Publish(SosTriggeredEvent)

    Handler-->>API: Result.Success(ComplaintDto)
    API-->>ResidentApp: 201 Created<br/>ComplaintDto (urgency: SOS_EMERGENCY, status: ASSIGNED)
    ResidentApp-->>Resident: SOS confirmed — "Emergency reported"

    Note over Worker: Async blast — ALL on-call staff notified simultaneously
    Worker->>DispatchSvc: FindOptimalStaffAsync(complaint, propertyId)
    DispatchSvc->>DB: SELECT StaffMembers WHERE availability=AVAILABLE AND propertyId=X
    DB-->>DispatchSvc: [staff1, staff2, ..., staffN]
    DispatchSvc-->>Worker: List<StaffScore> (ranked — all used for blast)

    Note over Worker: Fire notifications concurrently — NOT sequentially
    par Simultaneous blast to all available staff
        Worker->>NotifSvc: NotifyStaff(staff1, complaint)
        NotifSvc-->>Staff1: EMERGENCY ALERT — SMS + Push
    and
        Worker->>NotifSvc: NotifyStaff(staff2, complaint)
        NotifSvc-->>Staff2: EMERGENCY ALERT — SMS + Push
    and
        Worker->>NotifSvc: NotifyStaff(staffN, complaint)
        NotifSvc-->>StaffN: EMERGENCY ALERT — SMS + Push
    end

    Worker->>DB: WriteAuditEntry(SosTriggered)

    Note over Worker: Manager dashboard updates in real-time<br/>(polling or next dashboard refresh)
```

---

## Diagram 5 — Declare Outage (with Mass Broadcast)

**Trigger:** Manager declares a property-wide outage via `POST /api/v1/outages`.

**Key constraint:** The HTTP response must return immediately after the outage record is persisted. The mass notification broadcast to all residents is asynchronous and must not block the response. The broadcast must complete within 60 seconds for a property of 5,000 units (NFR-12). Notifications are dispatched concurrently, not sequentially.

```mermaid
sequenceDiagram
    actor Manager
    participant Dashboard as ResidentApp.Web<br/>(Manager Dashboard)
    participant API as ACLS.Api<br/>OutagesController
    participant TenancyMW as TenancyMiddleware
    participant Mediator as MediatR
    participant TxBehaviour as TransactionBehaviour
    participant Handler as DeclareOutageCommandHandler
    participant OutageRepo as IOutageRepository
    participant DB as AclsDbContext<br/>(MSSQL)
    participant Publisher as IPublisher
    participant Worker as ACLS.Worker
    participant ResidentRepo as IResidentRepository
    participant NotifSvc as INotificationService
    participant Resident1 as Resident 1
    participant Resident2 as Resident 2
    participant ResidentN as Resident N

    Manager->>Dashboard: Fills outage form<br/>(type, title, description, startTime, endTime)
    Dashboard->>API: POST /api/v1/outages<br/>Body: { title, outageType, description,<br/>        startTime, endTime }

    API->>TenancyMW: Extract property_id from JWT
    TenancyMW-->>API: ICurrentPropertyContext (PropertyId, UserId)

    API->>Mediator: Send(DeclareOutageCommand)

    Note over Mediator: ValidationBehaviour — validates times, required fields

    Mediator->>TxBehaviour: Begin transaction
    TxBehaviour->>Handler: Handle(command, ct)

    Handler->>Handler: Outage.Create(title, outageType, description,<br/>startTime, endTime, propertyId, declaredByUserId)<br/>→ raises OutageDeclaredEvent

    Handler->>OutageRepo: AddAsync(outage)
    DB-->>Handler: Saved (OutageId assigned)<br/>NotificationSentAt = null

    TxBehaviour->>DB: COMMIT TRANSACTION

    Handler->>Publisher: Publish(OutageDeclaredEvent)
    Publisher-->>Handler: Event queued for Worker

    Handler-->>API: Result.Success(OutageDto)

    API-->>Dashboard: 201 Created<br/>OutageDto (notificationSentAt: null)

    Note over Dashboard: Manager sees outage created.<br/>notificationSentAt will populate<br/>asynchronously.
    Dashboard-->>Manager: "Outage declared — notifications sending"

    Note over Worker: Async broadcast — runs after HTTP response returned
    Worker->>Worker: BroadcastOutageNotificationJob triggered<br/>by OutageDeclaredEvent

    Worker->>ResidentRepo: GetAllByPropertyAsync(propertyId)
    DB-->>Worker: [resident1, resident2, ..., residentN]<br/>(all residents of the property)

    Note over Worker: NFR-12 — 500 messages within 60 seconds<br/>Concurrent dispatch, not sequential loop

    par Concurrent notification dispatch
        Worker->>NotifSvc: SendOutageAlert(resident1, outage)<br/>(Email + SMS)
        NotifSvc-->>Resident1: "Planned Electricity Outage 7pm-7:30pm"
    and
        Worker->>NotifSvc: SendOutageAlert(resident2, outage)
        NotifSvc-->>Resident2: "Planned Electricity Outage 7pm-7:30pm"
    and
        Worker->>NotifSvc: SendOutageAlert(residentN, outage)
        NotifSvc-->>ResidentN: "Planned Electricity Outage 7pm-7:30pm"
    end

    Note over Worker: All notifications dispatched
    Worker->>DB: UPDATE Outages<br/>SET notification_sent_at = UtcNow<br/>WHERE OutageId = X

    Worker->>DB: WriteAuditEntry(OutageDeclared)

    Note over Dashboard: Manager refreshes / polls dashboard<br/>notificationSentAt now populated
```

---

## Cross-Cutting Notes

### Async Event Pattern

All five diagrams follow the same pattern for post-commit side effects:

1. The command handler publishes a domain event after the transaction commits.
2. The HTTP response is returned immediately after the event is published.
3. `ACLS.Worker` handles the event asynchronously.
4. Notifications, TAT calculations, and audit writes happen in the Worker — never inline in the handler.

This ensures the API response time is never held hostage by notification delivery speed, and that a slow SMS provider cannot cause an HTTP timeout.

### Transaction Boundary Rule

The transaction in Diagrams 2 and 3 spans exactly the two database writes that must be atomic:

| Diagram | Write 1 | Write 2 |
|---|---|---|
| Assign Complaint | `Complaints.Status = ASSIGNED` | `StaffMembers.Availability = BUSY` |
| Resolve Complaint | `Complaints.Status = RESOLVED` | `StaffMembers.Availability = AVAILABLE` |

The domain event publish (`Publisher.Publish(...)`) happens inside the handler but after both writes, before the transaction commits in Diagram 2 and after commit in Diagram 3. In both cases the event is only processed by the Worker if the transaction has committed — events are not processed for rolled-back transactions.

### 404 on Cross-Property Access

In Diagrams 2 and 3, the `GetByIdAsync` calls include `propertyId`. If the complaint or staff member belongs to a different property, the repository returns `null` and the handler returns `Result.Failure(NotFound)` — which maps to HTTP 404. The caller cannot determine whether the resource exists but is inaccessible (403) or simply does not exist (404). This is intentional — see `docs/07_Implementation/patterns/multi_tenancy_pattern.md` Section 7.

---

*End of Sequence Diagrams v1.0*
