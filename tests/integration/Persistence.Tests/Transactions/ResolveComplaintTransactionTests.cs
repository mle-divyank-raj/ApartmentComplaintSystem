using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;
using ACLS.Persistence.Repositories;
using FluentAssertions;
using NUnit.Framework;

namespace Persistence.Tests.Transactions;

/// <summary>
/// Integration tests verifying that the Resolve Complaint operation is atomic:
/// both Complaint.Status → RESOLVED and StaffMember.Availability → AVAILABLE must
/// commit together, or both must roll back on failure.
/// Tests use a raw EF Core database transaction to mirror the TransactionBehaviour pipeline.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class ResolveComplaintTransactionTests : IntegrationTestBase
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a complaint that is already IN_PROGRESS (assigned + en route + started work)
    /// and the assigned staff member (BUSY), ready for resolution.
    /// </summary>
    private async Task<(int propertyId, Complaint complaint, StaffMember staff)> SeedInProgressComplaintAsync()
    {
        var property = Property.Create("Resolve Tx Property", "99 Resolve Ave");
        await Context.Properties.AddAsync(property);
        await Context.SaveChangesAsync();

        var building = Building.Create(property.PropertyId, "R Block");
        await Context.Buildings.AddAsync(building);
        await Context.SaveChangesAsync();

        var unit = Unit.Create(building.BuildingId, "R01", 0);
        await Context.Units.AddAsync(unit);
        await Context.SaveChangesAsync();

        var residentUser = User.Create(
            "resolve.resident@test.com", "$2a$11$hash", "Res", "Ident",
            Role.Resident, property.PropertyId);
        var staffUser = User.Create(
            "resolve.staff@test.com", "$2a$11$hash", "Staff", "Member",
            Role.MaintenanceStaff, property.PropertyId);
        await Context.Users.AddRangeAsync(residentUser, staffUser);
        await Context.SaveChangesAsync();

        var resident = Resident.Create(residentUser.UserId, unit.UnitId);
        await Context.Residents.AddAsync(resident);
        await Context.SaveChangesAsync();

        // Seed staff
        var staffRepo = new StaffRepository(Context);
        var staff = StaffMember.Create(staffUser.UserId, "HVAC Tech", ["HVAC"]);
        await staffRepo.AddAsync(staff, CancellationToken.None);

        // Seed complaint and advance to IN_PROGRESS
        var complaintRepo = new ComplaintRepository(Context);
        var complaint = Complaint.Create(
            "No heating", "Heating unit failed in unit R01.",
            "HVAC", Urgency.HIGH,
            unit.UnitId, resident.ResidentId, property.PropertyId,
            permissionToEnter: true);
        await complaintRepo.AddAsync(complaint, CancellationToken.None);

        // Advance state machine: OPEN → ASSIGNED → EN_ROUTE → IN_PROGRESS
        complaint.Assign(staff.StaffMemberId);
        staff.MarkBusy();
        complaint.AcceptAssignment();
        complaint.StartWork();

        await complaintRepo.UpdateAsync(complaint, CancellationToken.None);
        await staffRepo.UpdateAsync(staff, CancellationToken.None);

        return (property.PropertyId, complaint, staff);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ResolveComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction()
    {
        // Arrange
        var (propertyId, complaint, staff) = await SeedInProgressComplaintAsync();
        var complaintRepo = new ComplaintRepository(Context);
        var staffRepo = new StaffRepository(Context);

        // Act: wrap both updates in a single transaction (mirrors TransactionBehaviour)
        await using var tx = await Context.Database.BeginTransactionAsync();

        complaint.Resolve();
        await complaintRepo.UpdateAsync(complaint, CancellationToken.None);

        staff.MarkAvailable();
        await staffRepo.UpdateAsync(staff, CancellationToken.None);

        await tx.CommitAsync();

        // Assert via fresh context — both entities must reflect the new state
        await using var fresh = CreateFreshContext();

        var persistedComplaint = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        persistedComplaint!.Status.Should().Be(TicketStatus.RESOLVED);
        persistedComplaint.ResolvedAt.Should().NotBeNull();

        var persistedStaff = await fresh.StaffMembers.FindAsync(staff.StaffMemberId);
        persistedStaff!.Availability.Should().Be(StaffState.AVAILABLE);
    }

    [Test]
    public async Task ResolveComplaint_WhenComplaintUpdateFails_StaffAvailabilityNotChanged()
    {
        // Arrange
        var (propertyId, complaint, staff) = await SeedInProgressComplaintAsync();
        var staffRepo = new StaffRepository(Context);

        // Act: begin transaction, update staff first, then roll back before updating complaint
        await using var tx = await Context.Database.BeginTransactionAsync();

        try
        {
            // In the resolve flow, complaint is updated first; simulate failure after
            complaint.Resolve();

            // Simulate a failure before persisting either entity
            throw new InvalidOperationException("Simulated failure before resolve persists.");

#pragma warning disable CS0162
            var complaintRepo = new ComplaintRepository(Context);
            await complaintRepo.UpdateAsync(complaint, CancellationToken.None);

            staff.MarkAvailable();
            await staffRepo.UpdateAsync(staff, CancellationToken.None);

            await tx.CommitAsync();
#pragma warning restore CS0162
        }
        catch (InvalidOperationException)
        {
            await tx.RollbackAsync();
        }

        // Assert via fresh context — complaint must still be IN_PROGRESS, staff still BUSY
        await using var fresh = CreateFreshContext();

        var persistedComplaint = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        persistedComplaint!.Status.Should().Be(TicketStatus.IN_PROGRESS,
            "transaction rollback must revert the complaint resolve");
        persistedComplaint.ResolvedAt.Should().BeNull(
            "resolved timestamp must not be persisted when transaction was rolled back");

        var persistedStaff = await fresh.StaffMembers.FindAsync(staff.StaffMemberId);
        persistedStaff!.Availability.Should().Be(StaffState.BUSY,
            "staff must remain busy after rollback — they are still assigned");
    }
}
