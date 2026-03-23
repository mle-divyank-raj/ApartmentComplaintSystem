# Multi-Tenancy Pattern

**Document:** `docs/07_Implementation/patterns/multi_tenancy_pattern.md`  
**Version:** 1.0  
**Status:** Approved  
**Project:** Apartment Complaint Logging System (ACLS)

---

> [!IMPORTANT]
> Every repository method that touches a property-scoped entity must apply the `PropertyId` filter described in this document. A query that returns data without filtering by `PropertyId` is a multi-tenancy data breach. This pattern is not optional and has no exceptions. Read this document before writing any repository, any query handler, or any controller that touches persisted data.

---

## 1. The Core Problem

ACLS is a multi-tenant system. Multiple apartment properties are managed on the same platform, stored in the same database, served by the same API. The fundamental security requirement is:

> A user authenticated against Property 1 must never be able to see, modify, or infer the existence of data belonging to Property 2.

This is enforced through **row-level isolation** using a `PropertyId` discriminator column present on every property-scoped table. There are no separate schemas, no separate databases, and no query rewriting middleware. The isolation is implemented explicitly in every repository query.

---

## 2. The Three-Component Pattern

Multi-tenancy enforcement in ACLS is built from exactly three components. They work together in a fixed sequence on every authenticated request.

```
JWT Token
    │
    │  contains claim: property_id = "1"
    ▼
TenancyMiddleware                          [ACLS.Api]
    │
    │  extracts PropertyId from claim
    │  populates ICurrentPropertyContext (scoped)
    ▼
ICurrentPropertyContext                    [ACLS.Application]
    │
    │  exposes PropertyId = 1 to any handler that injects it
    ▼
Repository methods                         [ACLS.Persistence]
    │
    │  .Where(x => x.PropertyId == propertyId)
    │  applied on every property-scoped query
    ▼
SQL Server
    │
    │  Returns only rows WHERE property_id = 1
    ▼
Response contains only Property 1 data
```

---

## 3. Component 1 — `TenancyMiddleware`

**Location:** `ACLS.Api/Middleware/TenancyMiddleware.cs`

`TenancyMiddleware` runs after `UseAuthentication()` in the middleware pipeline. It reads the `property_id` claim from the authenticated JWT and populates the scoped `ICurrentPropertyContext` service.

```csharp
// ACLS.Api/Middleware/TenancyMiddleware.cs
namespace ACLS.Api.Middleware;

public sealed class TenancyMiddleware
{
    private readonly RequestDelegate _next;

    public TenancyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentPropertyContext propertyContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var propertyIdClaim = context.User.FindFirst("property_id")?.Value;
            var userIdClaim = context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(propertyIdClaim) ||
                !int.TryParse(propertyIdClaim, out var propertyId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    errorCode = "Auth.MissingPropertyClaim",
                    detail = "JWT does not contain a valid property_id claim."
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(userIdClaim) ||
                !int.TryParse(userIdClaim, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            ((CurrentPropertyContext)propertyContext).SetContext(propertyId, userId);
        }

        await _next(context);
    }
}
```

**Registration order in `Program.cs` (mandatory):**
```csharp
app.UseAuthentication();     // Must come first — validates JWT
app.UseAuthorization();      // Must come second — checks role claims
app.UseMiddleware<TenancyMiddleware>();  // Must come after auth — reads validated claims
app.MapControllers();
```

`TenancyMiddleware` must run after `UseAuthentication()`. If it runs before, `context.User` is not populated and the claim read will return null.

---

## 4. Component 2 — `ICurrentPropertyContext`

**Interface location:** `ACLS.Application/Common/Interfaces/ICurrentPropertyContext.cs`  
**Implementation location:** `ACLS.Api/Services/CurrentPropertyContext.cs`

The interface is defined in `ACLS.Application` so that command and query handlers can depend on it without referencing `ACLS.Api`. The implementation is in `ACLS.Api` because it depends on `HttpContext`.

```csharp
// ACLS.Application/Common/Interfaces/ICurrentPropertyContext.cs
namespace ACLS.Application.Common.Interfaces;

public interface ICurrentPropertyContext
{
    int PropertyId { get; }
    int UserId { get; }
}
```

```csharp
// ACLS.Api/Services/CurrentPropertyContext.cs
namespace ACLS.Api.Services;

public sealed class CurrentPropertyContext : ICurrentPropertyContext
{
    private int _propertyId;
    private int _userId;
    private bool _isSet;

    public int PropertyId
    {
        get
        {
            if (!_isSet) throw new InvalidOperationException(
                "PropertyId accessed before TenancyMiddleware has set it. " +
                "Ensure TenancyMiddleware runs before the handler.");
            return _propertyId;
        }
    }

    public int UserId
    {
        get
        {
            if (!_isSet) throw new InvalidOperationException(
                "UserId accessed before TenancyMiddleware has set it.");
            return _userId;
        }
    }

    internal void SetContext(int propertyId, int userId)
    {
        _propertyId = propertyId;
        _userId = userId;
        _isSet = true;
    }
}
```

**Registration in `Program.cs`:**
```csharp
builder.Services.AddScoped<ICurrentPropertyContext, CurrentPropertyContext>();
```

The `Scoped` lifetime is mandatory. A new instance is created per HTTP request, so `PropertyId` is always the value from the current request's JWT — never leaked from a previous request.

---

## 5. Component 3 — Repository Pattern with PropertyId

**All repository interfaces in `ACLS.Domain` must include `propertyId` as a parameter on every method that queries property-scoped data.**

### 5.1 Interface Convention

```csharp
// ACLS.Domain/Complaints/IComplaintRepository.cs
namespace ACLS.Domain.Complaints;

public interface IComplaintRepository
{
    // PropertyId is always the second parameter after the entity identifier
    Task<Complaint?> GetByIdAsync(int complaintId, int propertyId, CancellationToken ct);

    Task<IReadOnlyList<Complaint>> GetAllAsync(
        int propertyId,
        ComplaintQueryOptions? options,  // filtering, sorting, pagination
        CancellationToken ct);

    Task<IReadOnlyList<Complaint>> GetByResidentAsync(
        int residentId,
        int propertyId,
        CancellationToken ct);

    Task<IReadOnlyList<Complaint>> GetByStaffMemberAsync(
        int staffMemberId,
        int propertyId,
        CancellationToken ct);

    Task<IReadOnlyList<Complaint>> GetByUnitAsync(
        int unitId,
        int propertyId,
        CancellationToken ct);

    Task AddAsync(Complaint complaint, CancellationToken ct);

    // Update and Delete do not need propertyId — the entity already
    // carries PropertyId, and EF Core tracks it. The handler must
    // have already loaded the entity with propertyId validation.
    Task UpdateAsync(Complaint complaint, CancellationToken ct);
}
```

### 5.2 Implementation Convention

Every implementation in `ACLS.Persistence` must apply `PropertyId` in the `WHERE` clause. No exceptions.

```csharp
// ACLS.Persistence/Repositories/ComplaintRepository.cs
namespace ACLS.Persistence.Repositories;

public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly AclsDbContext _context;

    public ComplaintRepository(AclsDbContext context) => _context = context;

    public async Task<Complaint?> GetByIdAsync(
        int complaintId,
        int propertyId,
        CancellationToken ct)
        => await _context.Complaints
            .Include(c => c.Media)
            .Include(c => c.WorkNotes)
            .Where(c => c.ComplaintId == complaintId
                     && c.PropertyId == propertyId)  // ← MANDATORY
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Complaint>> GetAllAsync(
        int propertyId,
        ComplaintQueryOptions? options,
        CancellationToken ct)
    {
        var query = _context.Complaints
            .Where(c => c.PropertyId == propertyId);  // ← MANDATORY — applied first

        // Filtering applied after the PropertyId filter
        if (options?.Status is not null)
            query = query.Where(c => c.Status == options.Status);

        if (options?.Urgency is not null)
            query = query.Where(c => c.Urgency == options.Urgency);

        if (options?.Category is not null)
            query = query.Where(c => c.Category == options.Category);

        if (options?.DateFrom is not null)
            query = query.Where(c => c.CreatedAt >= options.DateFrom);

        if (options?.DateTo is not null)
            query = query.Where(c => c.CreatedAt <= options.DateTo);

        if (!string.IsNullOrWhiteSpace(options?.Search))
            query = query.Where(c =>
                c.Title.Contains(options.Search) ||
                c.Description.Contains(options.Search));

        // Sorting
        query = options?.SortBy switch
        {
            "urgency" => options.SortDirection == "asc"
                ? query.OrderBy(c => c.Urgency)
                : query.OrderByDescending(c => c.Urgency),
            "status" => options.SortDirection == "asc"
                ? query.OrderBy(c => c.Status)
                : query.OrderByDescending(c => c.Status),
            "updatedAt" => options.SortDirection == "asc"
                ? query.OrderBy(c => c.UpdatedAt)
                : query.OrderByDescending(c => c.UpdatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)  // default
        };

        // Pagination
        var page = options?.Page ?? 1;
        var pageSize = options?.PageSize ?? 20;
        query = query.Skip((page - 1) * pageSize).Take(pageSize);

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(Complaint complaint, CancellationToken ct)
    {
        await _context.Complaints.AddAsync(complaint, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Complaint complaint, CancellationToken ct)
    {
        _context.Complaints.Update(complaint);
        await _context.SaveChangesAsync(ct);
    }
}
```

### 5.3 All Other Repositories Follow the Same Pattern

Every property-scoped repository follows this exact pattern. The `PropertyId` filter is always the first `Where` clause applied.

```csharp
// StaffRepository — same pattern
public async Task<StaffMember?> GetByIdAsync(
    int staffMemberId,
    int propertyId,
    CancellationToken ct)
    => await _context.StaffMembers
        .Include(s => s.User)
        .Where(s => s.StaffMemberId == staffMemberId
                 && s.User.PropertyId == propertyId)  // ← PropertyId via User navigation
        .FirstOrDefaultAsync(ct);

public async Task<IReadOnlyList<StaffMember>> GetAvailableAsync(
    int propertyId,
    CancellationToken ct)
    => await _context.StaffMembers
        .Include(s => s.User)
        .Where(s => s.User.PropertyId == propertyId        // ← MANDATORY
                 && s.Availability == StaffState.AVAILABLE)
        .ToListAsync(ct);
```

**Note on `StaffMembers`:** `StaffMember` does not directly carry `PropertyId` — it is on the associated `User`. Queries that need property scoping on `StaffMembers` join through `User.PropertyId`. The index on `Users.PropertyId` covers this join efficiently.

---

## 6. How Handlers Use `ICurrentPropertyContext`

Command and query handlers inject `ICurrentPropertyContext` and pass `PropertyId` to every repository call. They never construct `PropertyId` from request parameters.

```csharp
// Correct handler pattern
internal sealed class GetAllComplaintsQueryHandler
    : IRequestHandler<GetAllComplaintsQuery, Result<ComplaintsPageDto>>
{
    private readonly IComplaintRepository _repository;
    private readonly ICurrentPropertyContext _propertyContext;  // ← injected

    public GetAllComplaintsQueryHandler(
        IComplaintRepository repository,
        ICurrentPropertyContext propertyContext)
    {
        _repository = repository;
        _propertyContext = propertyContext;
    }

    public async Task<Result<ComplaintsPageDto>> Handle(
        GetAllComplaintsQuery query,
        CancellationToken ct)
    {
        // PropertyId comes from the injected context — never from query parameters
        var complaints = await _repository.GetAllAsync(
            _propertyContext.PropertyId,  // ← always from context
            query.Options,
            ct);

        return Result.Success(ComplaintsPageDto.From(complaints));
    }
}
```

**What a handler must NEVER do:**
```csharp
// WRONG — PropertyId from request parameter
var complaints = await _repository.GetAllAsync(query.PropertyId, ...);

// WRONG — PropertyId hardcoded
var complaints = await _repository.GetAllAsync(1, ...);

// WRONG — no PropertyId at all
var complaints = await _context.Complaints.ToListAsync();
```

---

## 7. The 404 vs 403 Rule for Access Violations

When a user requests a resource that exists but belongs to a different property, the API returns `404 Not Found` — not `403 Forbidden`.

**Why:** Returning `403 Forbidden` confirms to the caller that the resource exists, which enables ID enumeration attacks. A user should not be able to determine whether a `complaintId` from another property exists.

**How it works naturally:** Because every repository query filters by `PropertyId`, a request for `complaintId = 42` from a user on Property 2 returns `null` from the repository (the row exists but does not match the `PropertyId` filter). The handler sees `null` and returns `Result.Failure(ComplaintErrors.NotFound(42))`. The controller maps this to `404 Not Found`.

No special "access denied" check is needed. The `PropertyId` filter handles it transparently.

```csharp
// Handler — NotFound is the correct result for cross-property access
var complaint = await _repository.GetByIdAsync(
    query.ComplaintId,
    _propertyContext.PropertyId,
    ct);

if (complaint is null)
    return Result.Failure<ComplaintDto>(ComplaintErrors.NotFound(query.ComplaintId));
    // Returns 404 — correct for both "does not exist" AND "belongs to another property"
```

---

## 8. Tables That Are NOT Property-Scoped

A small number of tables do not carry `PropertyId` and therefore do not require the filter. These are intentionally cross-property or property-root entities.

| Table | Why no PropertyId filter |
|---|---|
| `Properties` | The root entity. Queried only during authentication to verify the property exists. Never exposed to property-scoped user queries. |
| `Buildings` | Queried only through `Property` hierarchy traversal, which is already scoped. |
| `Units` | Same as Buildings. |
| `AuditLog` | `PropertyId` is nullable. System events may be cross-property. Read only by internal admin tooling, never exposed to property-scoped users. |

All other tables (`Users`, `Residents`, `StaffMembers`, `Complaints`, `Media`, `WorkNotes`, `InvitationTokens`, `Outages`) are property-scoped and require the `PropertyId` filter on every query.

---

## 9. Integration Test Pattern for Multi-Tenancy

Every repository integration test must include a cross-property isolation test. This test verifies that data from Property 2 is not returned when querying as Property 1.

```csharp
[Test]
public async Task GetByIdAsync_WhenComplaintBelongsToAnotherProperty_ReturnsNull()
{
    // Arrange
    var property1Id = 1;
    var property2Id = 2;

    // Seed a complaint belonging to Property 2
    var complaint = Complaint.Create(
        title: "Test",
        description: "Test description",
        urgency: Urgency.LOW,
        unitId: _property2Unit.UnitId,
        residentId: _property2Resident.ResidentId,
        propertyId: property2Id,
        permissionToEnter: false);

    await _context.Complaints.AddAsync(complaint);
    await _context.SaveChangesAsync();

    var repository = new ComplaintRepository(_context);

    // Act — query with Property 1's context
    var result = await repository.GetByIdAsync(
        complaint.ComplaintId,
        property1Id,    // ← wrong property — should return null
        CancellationToken.None);

    // Assert
    Assert.That(result, Is.Null,
        "A complaint from Property 2 must not be visible to Property 1.");
}
```

This test must exist for every repository that queries property-scoped data. It is the primary safeguard against multi-tenancy regressions.

---

## 10. Checklist — Before Committing Any Repository Method

Run through this checklist before considering any repository method complete:

- [ ] Does the method accept `int propertyId` as a parameter?
- [ ] Is `.Where(x => x.PropertyId == propertyId)` the first filter applied in the query?
- [ ] If the entity gets `PropertyId` via a navigation property (e.g. `StaffMember` via `User`), is the join and filter correct?
- [ ] Is there an integration test that verifies cross-property isolation for this method?
- [ ] Is `PropertyId` never sourced from the request body, route, or query string — only from `ICurrentPropertyContext`?

If any checkbox is unchecked, the method is not complete.

---

*End of Multi-Tenancy Pattern v1.0*
