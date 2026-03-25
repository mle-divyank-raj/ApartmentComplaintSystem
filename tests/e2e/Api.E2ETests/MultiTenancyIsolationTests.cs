using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ACLS.Domain.Identity;
using ACLS.Domain.Properties;
using ACLS.Domain.Residents;
using ACLS.Domain.Staff;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ACLS.Persistence;

namespace Api.E2ETests;

/// <summary>
/// End-to-end tests verifying that multi-tenancy isolation is enforced at the HTTP layer.
/// A user from Property 1 must not be able to read, modify, or act on resources
/// belonging to Property 2. All cross-property requests must return 404 Not Found.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class MultiTenancyIsolationTests : E2ETestBase
{
    /// <summary>
    /// Seeds a full complaint for a given property and returns its complaintId.
    /// </summary>
    private async Task<int> SeedComplaintForPropertyAsync(string emailSuffix)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        var property = Property.Create($"Isolation Property {emailSuffix}", $"{emailSuffix} Isolation St");
        await db.Properties.AddAsync(property);
        await db.SaveChangesAsync();

        var building = Building.Create(property.PropertyId, "Iso Block");
        await db.Buildings.AddAsync(building);
        await db.SaveChangesAsync();

        var unit = Unit.Create(building.BuildingId, "I01", 0);
        await db.Units.AddAsync(unit);
        await db.SaveChangesAsync();

        var residentUser = User.Create(
            $"iso.resident.{emailSuffix}@test.com", "$2a$11$hash",
            "Iso", "Resident", Role.Resident, property.PropertyId);
        await db.Users.AddAsync(residentUser);
        await db.SaveChangesAsync();

        var resident = Resident.Create(residentUser.UserId, unit.UnitId);
        await db.Residents.AddAsync(resident);
        await db.SaveChangesAsync();

        // Add a complaint directly to the DB seeded under this property
        var complaint = ACLS.Domain.Complaints.Complaint.Create(
            "Isolation test complaint",
            "This complaint belongs to a specific property.",
            "Plumbing",
            ACLS.Domain.Complaints.Urgency.LOW,
            unit.UnitId,
            resident.ResidentId,
            property.PropertyId,
            permissionToEnter: false);
        await db.Complaints.AddAsync(complaint);
        await db.SaveChangesAsync();

        return complaint.ComplaintId;
    }

    private async Task<(int staffMemberId, int propertyId)> SeedStaffForPropertyAsync(string emailSuffix)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        var property = Property.Create($"Staff Iso Property {emailSuffix}", $"{emailSuffix} Staff Iso St");
        await db.Properties.AddAsync(property);
        await db.SaveChangesAsync();

        var staffUser = User.Create(
            $"iso.staff.{emailSuffix}@test.com", "$2a$11$hash",
            "Iso", "Staff", Role.MaintenanceStaff, property.PropertyId);
        await db.Users.AddAsync(staffUser);
        await db.SaveChangesAsync();

        var staff = StaffMember.Create(staffUser.UserId, "Plumber", ["Plumbing"]);
        await db.StaffMembers.AddAsync(staff);
        await db.SaveChangesAsync();

        return (staff.StaffMemberId, property.PropertyId);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetComplaint_WhenComplaintBelongsToDifferentProperty_Returns404()
    {
        // Arrange: seed a complaint belonging to Property A
        var complaintId = await SeedComplaintForPropertyAsync("A");

        // Create a separate Property B user — they should NOT see Property A's complaint
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        var propertyB = Property.Create("Property B", "2 Property B Lane");
        await db.Properties.AddAsync(propertyB);
        await db.SaveChangesAsync();

        var managerB = User.Create("manager.b@test.com", "$2a$11$hash", "Mgr", "B", Role.Manager, propertyB.PropertyId);
        await db.Users.AddAsync(managerB);
        await db.SaveChangesAsync();

        var clientB = CreateAuthenticatedClient(managerB.UserId, propertyB.PropertyId, Role.Manager);

        // Act: Property B manager tries to access Property A's complaint
        var response = await clientB.GetAsync($"/api/v1/complaints/{complaintId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "cross-property complaint access must return 404");
    }

    [Test]
    public async Task GetAllComplaints_ReturnsOnlyAuthenticatedPropertyComplaints()
    {
        // Arrange: seed one complaint per property
        var complaintIdA = await SeedComplaintForPropertyAsync("C");
        var complaintIdB = await SeedComplaintForPropertyAsync("D");

        // Retrieve the propertyId for property C's complaint
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AclsDbContext>();

        var complaintA = await db.Complaints.FindAsync(complaintIdA);
        complaintA.Should().NotBeNull();
        var propertyAId = complaintA!.PropertyId;

        var managerA = User.Create("mgr.getall@test.com", "$2a$11$hash", "Mgr", "GetAll", Role.Manager, propertyAId);
        await db.Users.AddAsync(managerA);
        await db.SaveChangesAsync();

        var clientA = CreateAuthenticatedClient(managerA.UserId, propertyAId, Role.Manager);

        // Act
        var response = await clientA.GetAsync("/api/v1/complaints");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body).RootElement;

        // Response is paginated — check items array
        var items = json.GetProperty("items");
        items.GetArrayLength().Should().Be(1,
            "only complaints from the authenticated property should be returned");

        var first = items[0];
        first.GetProperty("complaintId").GetInt32().Should().Be(complaintIdA);
    }

    [Test]
    public async Task AssignComplaint_WhenStaffBelongsToDifferentProperty_Returns404()
    {
        // Arrange: complaint in Property E, staff in Property F
        var (propertyId, unitId, residentUserId, residentId, _, _, managerUserId)
            = await SeedComplaintPrerequisitesAsync();

        // Seed a complaint for property E
        var residentClient = CreateAuthenticatedClient(residentUserId, propertyId, Role.Resident);
        using var submitContent = new MultipartFormDataContent();
        submitContent.Add(new StringContent("Cross-property assign test"), "title");
        submitContent.Add(new StringContent("Testing that cross-property assignment is blocked."), "description");
        submitContent.Add(new StringContent("Plumbing"), "category");
        submitContent.Add(new StringContent("LOW"), "urgency");
        submitContent.Add(new StringContent("true"), "permissionToEnter");

        var submitResponse = await residentClient.PostAsync("/api/v1/complaints", submitContent);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var complaintId = JsonDocument.Parse(await submitResponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("complaintId").GetInt32();

        // Seed staff belonging to a DIFFERENT property (Property F)
        var (crossPropertyStaffId, _) = await SeedStaffForPropertyAsync("F");

        // Act: Manager from Property E tries to assign cross-property staff
        var managerClient = CreateAuthenticatedClient(managerUserId, propertyId, Role.Manager);
        var assignRequest = new { staffMemberId = crossPropertyStaffId };
        var assignResponse = await managerClient.PostAsJsonAsync(
            $"/api/v1/complaints/{complaintId}/assign", assignRequest);

        // Assert: the handler cannot find the cross-property staff member → 404
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "assigning staff from a different property must return 404");
    }
}
