using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;
using ACLS.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Persistence.Tests.Transactions;

/// <summary>
/// Integration tests verifying that the Assign Complaint operation is atomic:
/// both Complaint.Status → ASSIGNED and StaffMember.Availability → BUSY must
/// commit together, or both must roll back on failure.
/// Tests use a raw EF Core database transaction to mirror the TransactionBehaviour pipeline.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class AssignComplaintTransactionTests : IntegrationTestBase
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(int propertyId, Complaint complaint, StaffMember staff)> SeedAssignmentDataAsync()
    {
        var property = Property.Create("Transaction Test Property", "1 Tx Street");
        await Context.Properties.AddAsync(property);
        await Context.SaveChangesAsync();

        var building = Building.Create(property.PropertyId, "Block T");
        await Context.Buildings.AddAsync(building);
        await Context.SaveChangesAsync();

        var unit = Unit.Create(building.BuildingId, "T01", 1);
        await Context.Units.AddAsync(unit);
        await Context.SaveChangesAsync();

        var residentUser = User.Create(
            "tx.resident@test.com", "$2a$11$hash", "Tx", "Resident",
            Role.Resident, property.PropertyId);
        var staffUser = User.Create(
            "tx.staff@test.com", "$2a$11$hash", "Tx", "Staff",
            Role.MaintenanceStaff, property.PropertyId);
        await Context.Users.AddRangeAsync(residentUser, staffUser);
        await Context.SaveChangesAsync();

        var resident = Resident.Create(residentUser.UserId, unit.UnitId);
        await Context.Residents.AddAsync(resident);
        await Context.SaveChangesAsync();

        var complaint = Complaint.Create(
            "Broken window", "Second floor window broken.",
            "Glazing", Urgency.HIGH,
            unit.UnitId, resident.ResidentId, property.PropertyId,
            permissionToEnter: false);
        var complaintRepo = new ComplaintRepository(Context);
        await complaintRepo.AddAsync(complaint, CancellationToken.None);

        var staffRepo = new StaffRepository(Context);
        var staff = StaffMember.Create(staffUser.UserId, "Glazier", ["Glazing"]);
        await staffRepo.AddAsync(staff, CancellationToken.None);

        return (property.PropertyId, complaint, staff);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task AssignComplaint_WhenSuccessful_BothComplaintAndStaffUpdatedInSameTransaction()
    {
        // Arrange
        var (propertyId, complaint, staff) = await SeedAssignmentDataAsync();
        var complaintRepo = new ComplaintRepository(Context);
        var staffRepo = new StaffRepository(Context);

        // Act: wrap both updates in a single transaction (mirrors TransactionBehaviour)
        await using var tx = await Context.Database.BeginTransactionAsync();

        complaint.Assign(staff.StaffMemberId);
        await complaintRepo.UpdateAsync(complaint, CancellationToken.None);

        staff.MarkBusy();
        await staffRepo.UpdateAsync(staff, CancellationToken.None);

        await tx.CommitAsync();

        // Assert via fresh context — both entities must reflect the new state
        await using var fresh = CreateFreshContext();

        var persistedComplaint = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        persistedComplaint!.Status.Should().Be(TicketStatus.ASSIGNED);
        persistedComplaint.AssignedStaffMemberId.Should().Be(staff.StaffMemberId);

        var persistedStaff = await fresh.StaffMembers.FindAsync(staff.StaffMemberId);
        persistedStaff!.Availability.Should().Be(StaffState.BUSY);
        persistedStaff.LastAssignedAt.Should().NotBeNull();
    }

    [Test]
    public async Task AssignComplaint_WhenStaffUpdateFails_ComplaintStatusNotChanged()
    {
        // Arrange
        var (propertyId, complaint, staff) = await SeedAssignmentDataAsync();
        var complaintRepo = new ComplaintRepository(Context);

        // Act: begin transaction, update complaint, then roll back before updating staff
        await using var tx = await Context.Database.BeginTransactionAsync();

        try
        {
            complaint.Assign(staff.StaffMemberId);
            await complaintRepo.UpdateAsync(complaint, CancellationToken.None);

            // Simulate a failure before the staff update (e.g. staff not found, or DB error)
            throw new InvalidOperationException("Simulated failure before staff update.");

#pragma warning disable CS0162
            staff.MarkBusy();
            var staffRepo = new StaffRepository(Context);
            await staffRepo.UpdateAsync(staff, CancellationToken.None);
            await tx.CommitAsync();
#pragma warning restore CS0162
        }
        catch (InvalidOperationException)
        {
            await tx.RollbackAsync();
        }

        // Assert via fresh context — complaint must still be OPEN, staff still AVAILABLE
        await using var fresh = CreateFreshContext();

        var persistedComplaint = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        persistedComplaint!.Status.Should().Be(TicketStatus.OPEN,
            "transaction rollback must revert the complaint status change");

        var persistedStaff = await fresh.StaffMembers.FindAsync(staff.StaffMemberId);
        persistedStaff!.Availability.Should().Be(StaffState.AVAILABLE,
            "staff must remain available after rollback");
    }
}
