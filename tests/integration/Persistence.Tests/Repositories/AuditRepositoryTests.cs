using ACLS.Domain.AuditLog;
using ACLS.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Persistence.Tests.Repositories;

/// <summary>
/// Integration tests for AuditRepository against a real SQL Server (TestContainers).
/// Audit entries are immutable — only AddAsync is exposed. Tests verify persistence only.
/// Naming convention: Method_Scenario_ExpectedOutcome
/// </summary>
[TestFixture]
public sealed class AuditRepositoryTests : IntegrationTestBase
{
    [Test]
    public async Task AddAsync_WithValidEntry_PersistsToDatabase()
    {
        // Arrange
        var entry = AuditEntry.Create(
            AuditAction.ComplaintCreated,
            entityType: "Complaint",
            entityId: 1,
            propertyId: 42,
            actorUserId: 7,
            actorRole: "Resident",
            newValue: "{\"status\":\"OPEN\"}",
            ipAddress: "127.0.0.1");

        var repo = new AuditRepository(Context);

        // Act
        await repo.AddAsync(entry, CancellationToken.None);

        // Assert via fresh context
        await using var fresh = CreateFreshContext();
        var persisted = await fresh.AuditLog
            .Where(e => e.AuditEntryId == entry.AuditEntryId)
            .FirstOrDefaultAsync();

        persisted.Should().NotBeNull();
        persisted!.Action.Should().Be(AuditAction.ComplaintCreated);
        persisted.EntityType.Should().Be("Complaint");
        persisted.EntityId.Should().Be(1);
        persisted.PropertyId.Should().Be(42);
        persisted.ActorUserId.Should().Be(7);
        persisted.ActorRole.Should().Be("Resident");
        persisted.NewValue.Should().Be("{\"status\":\"OPEN\"}");
        persisted.IpAddress.Should().Be("127.0.0.1");
        persisted.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task AddAsync_WithMultipleEntries_AllPersisted()
    {
        // Arrange
        var entry1 = AuditEntry.Create(AuditAction.ComplaintCreated, "Complaint", 10, propertyId: 1);
        var entry2 = AuditEntry.Create(AuditAction.ComplaintAssigned, "Complaint", 10, propertyId: 1, actorUserId: 5);
        var entry3 = AuditEntry.Create(AuditAction.ComplaintResolved, "Complaint", 10, propertyId: 1, actorUserId: 5);

        var repo = new AuditRepository(Context);

        // Act
        await repo.AddAsync(entry1, CancellationToken.None);
        await repo.AddAsync(entry2, CancellationToken.None);
        await repo.AddAsync(entry3, CancellationToken.None);

        // Assert
        await using var fresh = CreateFreshContext();
        var all = await fresh.AuditLog
            .Where(e => e.EntityId == 10 && e.PropertyId == 1)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();

        all.Should().HaveCount(3);
        all[0].Action.Should().Be(AuditAction.ComplaintCreated);
        all[1].Action.Should().Be(AuditAction.ComplaintAssigned);
        all[2].Action.Should().Be(AuditAction.ComplaintResolved);
    }

    [Test]
    public async Task AddAsync_WithSystemInitiatedEntry_PersistsWithNullPropertyAndActor()
    {
        // Arrange: system-initiated entries have no PropertyId or ActorUserId
        var entry = AuditEntry.Create(
            AuditAction.OutageDeclared,
            entityType: "Outage",
            entityId: 99);

        var repo = new AuditRepository(Context);

        // Act
        await repo.AddAsync(entry, CancellationToken.None);

        // Assert
        await using var fresh = CreateFreshContext();
        var persisted = await fresh.AuditLog
            .Where(e => e.AuditEntryId == entry.AuditEntryId)
            .FirstOrDefaultAsync();

        persisted.Should().NotBeNull();
        persisted!.PropertyId.Should().BeNull();
        persisted.ActorUserId.Should().BeNull();
        persisted.ActorRole.Should().BeNull();
    }
}
