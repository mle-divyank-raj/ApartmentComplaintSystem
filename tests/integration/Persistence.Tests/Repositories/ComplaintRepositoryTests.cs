using ACLS.Domain.Complaints;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Persistence.Repositories;
using FluentAssertions;
using NUnit.Framework;

namespace Persistence.Tests.Repositories;

/// <summary>
/// Integration tests for ComplaintRepository against a real SQL Server (TestContainers).
/// Every test verifies the PropertyId multi-tenancy filter is enforced.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class ComplaintRepositoryTests : IntegrationTestBase
{
    // ── Test Constants ───────────────────────────────────────────────────────

    private const int Property1Id = 1; // Seeded via Property.Create → DB assigns PK
    private const int Property2Id = 2; // Separate tenant — used for cross-property isolation tests

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds the minimal hierarchy needed to create a Complaint:
    /// Property → Building → Unit → User (Resident role) → Resident
    /// Returns the seeded IDs so tests can reference them.
    /// </summary>
    private async Task<(int propertyId, int unitId, int residentId)> SeedComplaintPrerequisitesAsync(
        string emailSuffix = "a")
    {
        var property = Property.Create($"Test Property {emailSuffix}", $"{emailSuffix} Test St");
        await Context.Properties.AddAsync(property);
        await Context.SaveChangesAsync();

        var building = Building.Create(property.PropertyId, "Block A");
        await Context.Buildings.AddAsync(building);
        await Context.SaveChangesAsync();

        var unit = Unit.Create(building.BuildingId, "101", 1);
        await Context.Units.AddAsync(unit);
        await Context.SaveChangesAsync();

        var user = User.Create(
            $"resident.{emailSuffix}@test.com",
            "$2a$11$hashedpassword",
            "Jane",
            "Doe",
            Role.Resident,
            property.PropertyId);
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();

        var resident = Resident.Create(user.UserId, unit.UnitId);
        await Context.Residents.AddAsync(resident);
        await Context.SaveChangesAsync();

        return (property.PropertyId, unit.UnitId, resident.ResidentId);
    }

    private static Complaint BuildComplaint(int unitId, int residentId, int propertyId)
        => Complaint.Create(
            "Leaking kitchen tap",
            "The kitchen tap has been dripping for two days.",
            "Plumbing",
            Urgency.MEDIUM,
            unitId,
            residentId,
            propertyId,
            permissionToEnter: true);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetByIdAsync_WhenComplaintExists_ReturnsComplaint()
    {
        // Arrange
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("b");
        var complaint = BuildComplaint(unitId, residentId, propertyId);
        var repo = new ComplaintRepository(Context);
        await repo.AddAsync(complaint, CancellationToken.None);

        // Act
        var result = await repo.GetByIdAsync(complaint.ComplaintId, propertyId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ComplaintId.Should().Be(complaint.ComplaintId);
        result.Title.Should().Be("Leaking kitchen tap");
        result.PropertyId.Should().Be(propertyId);
    }

    [Test]
    public async Task GetByIdAsync_WhenComplaintNotFound_ReturnsNull()
    {
        // Arrange
        var (propertyId, _, _) = await SeedComplaintPrerequisitesAsync("c");
        var repo = new ComplaintRepository(Context);

        // Act
        var result = await repo.GetByIdAsync(99999, propertyId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_WhenComplaintBelongsToDifferentProperty_ReturnsNull()
    {
        // Arrange: seed complaint under Property 2, query as Property 1
        var (property2Id, unitId, residentId) = await SeedComplaintPrerequisitesAsync("d");
        var (property1Id, _, _) = await SeedComplaintPrerequisitesAsync("e");

        var complaint = BuildComplaint(unitId, residentId, property2Id);
        var repo = new ComplaintRepository(Context);
        await repo.AddAsync(complaint, CancellationToken.None);

        // Act: query with Property 1's ID — must NOT see Property 2's complaint
        var result = await repo.GetByIdAsync(complaint.ComplaintId, property1Id, CancellationToken.None);

        // Assert
        result.Should().BeNull("cross-property access must return null");
    }

    [Test]
    public async Task GetAllAsync_WithPropertyFilter_ReturnsOnlyPropertyComplaints()
    {
        // Arrange: two properties, one complaint each
        var (propAId, unitAId, resAId) = await SeedComplaintPrerequisitesAsync("f");
        var (propBId, unitBId, resBId) = await SeedComplaintPrerequisitesAsync("g");

        var repo = new ComplaintRepository(Context);
        await repo.AddAsync(BuildComplaint(unitAId, resAId, propAId), CancellationToken.None);
        await repo.AddAsync(BuildComplaint(unitBId, resBId, propBId), CancellationToken.None);

        // Act: query only for Property A
        var results = await repo.GetAllAsync(propAId, null, CancellationToken.None);

        // Assert: exactly one result, belonging to Property A
        results.Should().HaveCount(1);
        results[0].PropertyId.Should().Be(propAId);
    }

    [Test]
    public async Task GetAllAsync_WithStatusFilter_ReturnsFilteredComplaints()
    {
        // Arrange
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("h");
        var repo = new ComplaintRepository(Context);

        var complaint1 = BuildComplaint(unitId, residentId, propertyId);
        var complaint2 = BuildComplaint(unitId, residentId, propertyId);
        await repo.AddAsync(complaint1, CancellationToken.None);
        await repo.AddAsync(complaint2, CancellationToken.None);

        // Manually transition complaint2 to SOS_EMERGENCY via TriggerSos
        complaint2.TriggerSos();
        await repo.UpdateAsync(complaint2, CancellationToken.None);

        // Act: filter by OPEN status
        var results = await repo.GetAllAsync(
            propertyId,
            new ComplaintQueryOptions(Status: TicketStatus.OPEN),
            CancellationToken.None);

        // Assert
        results.Should().HaveCount(1);
        results[0].Status.Should().Be(TicketStatus.OPEN);
    }

    [Test]
    public async Task GetAllAsync_WithDateRangeFilter_ReturnsFilteredComplaints()
    {
        // Arrange
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("i");
        var repo = new ComplaintRepository(Context);
        await repo.AddAsync(BuildComplaint(unitId, residentId, propertyId), CancellationToken.None);

        // Act: date range that includes today
        var results = await repo.GetAllAsync(
            propertyId,
            new ComplaintQueryOptions(
                DateFrom: DateTime.UtcNow.AddDays(-1),
                DateTo: DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        results.Should().HaveCount(1);

        // Act: date range in the past — should return nothing
        var emptyResults = await repo.GetAllAsync(
            propertyId,
            new ComplaintQueryOptions(
                DateFrom: DateTime.UtcNow.AddDays(-10),
                DateTo: DateTime.UtcNow.AddDays(-2)),
            CancellationToken.None);

        emptyResults.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange: seed 5 complaints
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("j");
        var repo = new ComplaintRepository(Context);

        for (var i = 0; i < 5; i++)
            await repo.AddAsync(BuildComplaint(unitId, residentId, propertyId), CancellationToken.None);

        // Act: page 2, page size 2
        var results = await repo.GetAllAsync(
            propertyId,
            new ComplaintQueryOptions(Page: 2, PageSize: 2),
            CancellationToken.None);

        // Assert
        results.Should().HaveCount(2);
    }

    [Test]
    public async Task AddAsync_WithValidComplaint_PersistsToDatabase()
    {
        // Arrange
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("k");
        var complaint = BuildComplaint(unitId, residentId, propertyId);
        var repo = new ComplaintRepository(Context);

        // Act
        await repo.AddAsync(complaint, CancellationToken.None);

        // Assert via fresh context to avoid cached reads
        await using var fresh = CreateFreshContext();
        var persisted = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be("Leaking kitchen tap");
        persisted.Status.Should().Be(TicketStatus.OPEN);
    }

    [Test]
    public async Task UpdateAsync_WhenComplaintStatusChanged_PersistsNewStatus()
    {
        // Arrange
        var (propertyId, unitId, residentId) = await SeedComplaintPrerequisitesAsync("l");
        var complaint = BuildComplaint(unitId, residentId, propertyId);
        var repo = new ComplaintRepository(Context);
        await repo.AddAsync(complaint, CancellationToken.None);

        // Act: trigger SOS state transition and persist
        complaint.TriggerSos();
        await repo.UpdateAsync(complaint, CancellationToken.None);

        // Assert via fresh context — TriggerSos sets Urgency=SOS_EMERGENCY, Status=ASSIGNED
        await using var fresh = CreateFreshContext();
        var updated = await fresh.Complaints.FindAsync(complaint.ComplaintId);
        updated!.Status.Should().Be(TicketStatus.ASSIGNED);
        updated.Urgency.Should().Be(Urgency.SOS_EMERGENCY);
    }
}
