using System.Reflection;
using ACLS.Domain.Complaints;
using ACLS.Domain.Outages;
using ACLS.Domain.Staff;

namespace Application.Tests;

/// <summary>
/// Centralised factory for creating domain objects in test state.
/// Uses reflection to set private properties when needed to simulate
/// states that are only reachable via specific domain method call sequences
/// (e.g. setting Status to IN_PROGRESS without calling Assign → AcceptAssignment → StartWork).
/// </summary>
internal static class TestDataFactory
{
    internal const int PropertyId     = 1;
    internal const int ResidentId     = 10;
    internal const int StaffMemberId  = 20;
    internal const int UnitId         = 5;
    internal const int ManagerUserId  = 30;

    // ─── Complaint ───────────────────────────────────────────────────────────

    /// <summary>Creates a Complaint in OPEN status by default.</summary>
    internal static Complaint CreateComplaint(
        TicketStatus status     = TicketStatus.OPEN,
        Urgency urgency         = Urgency.MEDIUM,
        int complaintId         = 1,
        int propertyId          = PropertyId,
        int residentId          = ResidentId,
        int unitId              = UnitId,
        int? assignedStaffId    = null)
    {
        var complaint = Complaint.Create(
            title:             "Leaking tap",
            description:       "The kitchen tap has been dripping for two days.",
            category:          "Plumbing",
            urgency:           urgency,
            unitId:            unitId,
            residentId:        residentId,
            propertyId:        propertyId,
            permissionToEnter: true,
            requiredSkills:    ["Plumbing"]);

        complaint.ClearDomainEvents();

        SetPrivateProperty(complaint, "ComplaintId", complaintId);

        if (status != TicketStatus.OPEN)
            SetPrivateProperty(complaint, "Status", status);

        if (assignedStaffId.HasValue)
            SetPrivateProperty(complaint, "AssignedStaffMemberId", assignedStaffId.Value);

        return complaint;
    }

    // ─── StaffMember ─────────────────────────────────────────────────────────

    /// <summary>Creates a StaffMember with AVAILABLE state by default.</summary>
    internal static StaffMember CreateStaff(
        StaffState availability = StaffState.AVAILABLE,
        List<string>? skills    = null,
        int staffMemberId       = StaffMemberId,
        int userId              = 200)
    {
        var staff = StaffMember.Create(
            userId:   userId,
            jobTitle: "Maintenance Technician",
            skills:   skills ?? ["Plumbing"]);

        staff.ClearDomainEvents();

        SetPrivateProperty(staff, "StaffMemberId", staffMemberId);

        if (availability != StaffState.AVAILABLE)
            SetPrivateProperty(staff, "Availability", availability);

        return staff;
    }

    // ─── Outage ──────────────────────────────────────────────────────────────

    /// <summary>Creates a declared Outage.</summary>
    internal static Outage CreateOutage(
        int outageId          = 1,
        int propertyId        = PropertyId,
        OutageType outageType = OutageType.Water)
    {
        var outage = Outage.Declare(
            propertyId:               propertyId,
            declaredByManagerUserId:  ManagerUserId,
            title:                    "Water outage for maintenance",
            outageType:               outageType,
            description:              "Planned maintenance. Water will be off for 2 hours.",
            startTime:                DateTime.UtcNow.AddHours(1),
            endTime:                  DateTime.UtcNow.AddHours(3));

        outage.ClearDomainEvents();

        SetPrivateProperty(outage, "OutageId", outageId);
        return outage;
    }

    // ─── Reflection helper ───────────────────────────────────────────────────

    /// <summary>
    /// Sets a private or private-set property on a domain entity via reflection.
    /// Used exclusively in tests to create entities in specific states without
    /// traversing the full domain state machine.
    /// </summary>
    internal static void SetPrivateProperty<TEntity>(TEntity entity, string propertyName, object value)
    {
        var property = typeof(TEntity).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        property!.SetValue(entity, value);
    }
}
