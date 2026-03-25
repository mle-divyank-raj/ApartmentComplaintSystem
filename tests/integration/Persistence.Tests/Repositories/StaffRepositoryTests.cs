using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Staff;
using ACLS.Persistence.Repositories;
using FluentAssertions;
using NUnit.Framework;

namespace Persistence.Tests.Repositories;

/// <summary>
/// Integration tests for StaffRepository against a real SQL Server (TestContainers).
/// StaffMember has no direct PropertyId — it scopes through User.PropertyId via a join.
/// Cross-property isolation tests verify that join-based scoping is correctly enforced.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class StaffRepositoryTests : IntegrationTestBase
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a Property + User (MaintenanceStaff) + StaffMember.
    /// Returns the seeded IDs.
    /// </summary>
    private async Task<(int propertyId, int staffMemberId)> SeedStaffAsync(string emailSuffix = "a")
    {
        var property = Property.Create($"Staff Test Property {emailSuffix}", $"{emailSuffix} Staff St");
        await Context.Properties.AddAsync(property);
        await Context.SaveChangesAsync();

        var user = User.Create(
            $"staff.{emailSuffix}@test.com",
            "$2a$11$hashedpassword",
            "John",
            "Wrench",
            Role.MaintenanceStaff,
            property.PropertyId);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var staff = StaffMember.Create(user.UserId, "Plumber", ["Plumbing", "HVAC"]);
        var staffRepo = new StaffRepository(Context);
        await staffRepo.AddAsync(staff, CancellationToken.None);

        return (property.PropertyId, staff.StaffMemberId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_WhenStaffExists_ReturnsStaffMember()
    {
        // Arrange
        var (propertyId, staffMemberId) = await SeedStaffAsync("b");
        var repo = new StaffRepository(Context);

        // Act
        var result = await repo.GetByIdAsync(staffMemberId, propertyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.StaffMemberId.Should().Be(staffMemberId);
        result.Availability.Should().Be(StaffState.AVAILABLE);
    }

    [Test]
    public async Task GetByIdAsync_WhenStaffBelongsToDifferentProperty_ReturnsNull()
    {
        // Arrange: seed staff under Property 2
        var (property2Id, staffMemberId) = await SeedStaffAsync("c");

        // Create a separate Property 1 for the querying tenant
        var property1 = Property.Create("Querying Property", "1 Query Lane");
        await Context.Properties.AddAsync(property1);
        await Context.SaveChangesAsync();

        var repo = new StaffRepository(Context);

        // Act: query with Property 1's ID — must NOT see Property 2's staff
        var result = await repo.GetByIdAsync(staffMemberId, property1.PropertyId, CancellationToken.None);

        // Assert
        result.Should().BeNull("cross-property access must return null");
    }

    [Test]
    public async Task GetAvailableAsync_ReturnsOnlyAvailableStaffForProperty()
    {
        // Arrange: two staff members in the same property; mark one busy
        var property = Property.Create("Availability Test Property", "1 Avail Ave");
        await Context.Properties.AddAsync(property);
        await Context.SaveChangesAsync();

        var user1 = User.Create("staff.avail1@test.com", "$2a$11$hash", "Alice", "Fix", Role.MaintenanceStaff, property.PropertyId);
        var user2 = User.Create("staff.avail2@test.com", "$2a$11$hash", "Bob", "Fix", Role.MaintenanceStaff, property.PropertyId);
        await Context.Users.AddRangeAsync(user1, user2);
        await Context.SaveChangesAsync();

        var staffRepo = new StaffRepository(Context);
        var staff1 = StaffMember.Create(user1.UserId, "Electrician");
        var staff2 = StaffMember.Create(user2.UserId, "Plumber");
        await staffRepo.AddAsync(staff1, CancellationToken.None);
        await staffRepo.AddAsync(staff2, CancellationToken.None);

        // Mark staff2 as busy
        staff2.MarkBusy();
        await staffRepo.UpdateAsync(staff2, CancellationToken.None);

        // Act
        var available = await staffRepo.GetAvailableAsync(property.PropertyId, CancellationToken.None);

        // Assert
        available.Should().HaveCount(1);
        available[0].StaffMemberId.Should().Be(staff1.StaffMemberId);
    }

    [Test]
    public async Task GetAvailableAsync_WhenStaffBelongsToDifferentProperty_ReturnsEmpty()
    {
        // Arrange: seed AVAILABLE staff under Property 2
        var (property2Id, _) = await SeedStaffAsync("d");

        // Separate Property 1 — no staff seeded here
        var property1 = Property.Create("Empty Property", "2 Empty Ln");
        await Context.Properties.AddAsync(property1);
        await Context.SaveChangesAsync();

        var repo = new StaffRepository(Context);

        // Act: query available staff for Property 1
        var available = await repo.GetAvailableAsync(property1.PropertyId, CancellationToken.None);

        // Assert: Property 1 sees no staff because staff belong to Property 2
        available.Should().BeEmpty("cross-property available staff must not be returned");
    }

    [Test]
    public async Task UpdateAsync_WhenAvailabilityChanged_PersistsNewAvailability()
    {
        // Arrange
        var (propertyId, staffMemberId) = await SeedStaffAsync("e");
        var repo = new StaffRepository(Context);
        var staff = await repo.GetByIdAsync(staffMemberId, propertyId, CancellationToken.None);
        staff.Should().NotBeNull();

        // Act
        staff!.MarkBusy();
        await repo.UpdateAsync(staff, CancellationToken.None);

        // Assert via fresh context
        await using var fresh = CreateFreshContext();
        var updated = await fresh.StaffMembers.FindAsync(staffMemberId);
        updated!.Availability.Should().Be(StaffState.BUSY);
        updated.LastAssignedAt.Should().NotBeNull();
    }
}
