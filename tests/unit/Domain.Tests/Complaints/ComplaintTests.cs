using ACLS.Domain.Complaints;
using ACLS.Domain.Complaints.Events;
using FluentAssertions;
using NUnit.Framework;

namespace Domain.Tests.Complaints;

/// <summary>
/// Unit tests for the Complaint aggregate root.
/// Tests verify domain invariants, state machine transitions, and domain event emission.
/// No database, no I/O — pure domain logic tests.
/// Coverage mandated by: docs/07_Implementation/testing_strategy.md Section 4.1
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public sealed class ComplaintTests
{
    // ── Factory helpers ───────────────────────────────────────────────────────

    private static Complaint CreateOpenComplaint(
        int propertyId = 1,
        int residentId = 10,
        int unitId = 5,
        List<string>? requiredSkills = null)
        => Complaint.Create(
            title: "Leaking pipe under kitchen sink",
            description: "Water pooling under the cabinet, possible burst pipe.",
            category: "Plumbing",
            urgency: Urgency.MEDIUM,
            unitId: unitId,
            residentId: residentId,
            propertyId: propertyId,
            permissionToEnter: true,
            requiredSkills: requiredSkills);

    private static Complaint CreateComplaintInStatus(
        TicketStatus targetStatus,
        int staffMemberId = 99)
    {
        var complaint = CreateOpenComplaint();

        if (targetStatus == TicketStatus.OPEN)
            return complaint;

        complaint.Assign(staffMemberId);

        if (targetStatus == TicketStatus.ASSIGNED)
            return complaint;

        complaint.AcceptAssignment();

        if (targetStatus == TicketStatus.EN_ROUTE)
            return complaint;

        complaint.StartWork();

        if (targetStatus == TicketStatus.IN_PROGRESS)
            return complaint;

        complaint.Resolve();

        if (targetStatus == TicketStatus.RESOLVED)
            return complaint;

        complaint.Close(residentRating: 4, feedbackComment: "Good job.");
        return complaint;
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public void Create_WithValidInputs_SetsStatusToOpen()
    {
        // Arrange / Act
        var complaint = CreateOpenComplaint();

        // Assert
        complaint.Status.Should().Be(TicketStatus.OPEN);
    }

    [Test]
    public void Create_WithValidInputs_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var complaint = CreateOpenComplaint();

        // Assert
        var after = DateTime.UtcNow;
        complaint.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        complaint.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ── Assign ────────────────────────────────────────────────────────────────

    [Test]
    public void Assign_WhenStatusIsOpen_SetsStatusToAssigned()
    {
        // Arrange
        var complaint = CreateOpenComplaint();

        // Act
        var result = complaint.Assign(staffMemberId: 42);

        // Assert
        result.IsSuccess.Should().BeTrue();
        complaint.Status.Should().Be(TicketStatus.ASSIGNED);
    }

    [Test]
    public void Assign_WhenStatusIsOpen_SetsAssignedStaffMemberId()
    {
        // Arrange
        var complaint = CreateOpenComplaint();
        const int staffMemberId = 42;

        // Act
        complaint.Assign(staffMemberId);

        // Assert
        complaint.AssignedStaffMemberId.Should().Be(staffMemberId);
    }

    [Test]
    public void Assign_WhenStatusIsOpen_RaisesComplaintAssignedEvent()
    {
        // Arrange
        var complaint = CreateOpenComplaint();
        const int staffMemberId = 42;

        // Act
        complaint.Assign(staffMemberId);

        // Assert
        complaint.DomainEvents.Should().ContainSingle(e => e is ComplaintAssignedEvent);
        var assignedEvent = (ComplaintAssignedEvent)complaint.DomainEvents
            .Single(e => e is ComplaintAssignedEvent);
        assignedEvent.AssignedStaffMemberId.Should().Be(staffMemberId);
    }

    [Test]
    public void Assign_WhenStatusIsClosed_ReturnsFailureResult()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.CLOSED);

        // Act
        var result = complaint.Assign(staffMemberId: 77);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }

    [Test]
    public void Assign_WhenStatusIsResolved_ReturnsFailureResult()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.RESOLVED);

        // Act
        var result = complaint.Assign(staffMemberId: 77);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }

    // ── Resolve ───────────────────────────────────────────────────────────────

    [Test]
    public void Resolve_WhenStatusIsInProgress_SetsStatusToResolved()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.IN_PROGRESS);

        // Act
        var result = complaint.Resolve();

        // Assert
        result.IsSuccess.Should().BeTrue();
        complaint.Status.Should().Be(TicketStatus.RESOLVED);
    }

    [Test]
    public void Resolve_WhenStatusIsInProgress_SetsResolvedAt()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.IN_PROGRESS);
        var before = DateTime.UtcNow;

        // Act
        complaint.Resolve();

        // Assert
        var after = DateTime.UtcNow;
        complaint.ResolvedAt.Should().NotBeNull();
        complaint.ResolvedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        complaint.ResolvedAt.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void Resolve_WhenStatusIsInProgress_RaisesComplaintResolvedEvent()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.IN_PROGRESS);
        complaint.ClearDomainEvents();

        // Act
        complaint.Resolve();

        // Assert
        complaint.DomainEvents.Should().Contain(e => e is ComplaintResolvedEvent);
    }

    [Test]
    public void Resolve_WhenStatusIsOpen_ReturnsFailureResult()
    {
        // Arrange
        var complaint = CreateOpenComplaint();

        // Act
        var result = complaint.Resolve();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    [Test]
    public void Close_WhenStatusIsResolved_SetsStatusToClosed()
    {
        // Arrange
        var complaint = CreateComplaintInStatus(TicketStatus.RESOLVED);

        // Act
        var result = complaint.Close(residentRating: 5, feedbackComment: "Excellent service.");

        // Assert
        result.IsSuccess.Should().BeTrue();
        complaint.Status.Should().Be(TicketStatus.CLOSED);
    }

    [Test]
    public void Close_WhenStatusIsOpen_ReturnsFailureResult()
    {
        // Arrange
        var complaint = CreateOpenComplaint();

        // Act
        var result = complaint.Close(residentRating: 3, feedbackComment: null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }

    // ── AddMedia ──────────────────────────────────────────────────────────────

    [Test]
    public void AddMedia_WhenUnderLimit_AddsMedia()
    {
        // Arrange
        var complaint = CreateOpenComplaint();
        var media = Media.Create(
            complaintId: 1,
            url: "https://blob.acls.io/evidence/photo1.jpg",
            type: "image/jpeg",
            uploadedByUserId: 10);

        // Act
        var result = complaint.AddMedia(media);

        // Assert
        result.IsSuccess.Should().BeTrue();
        complaint.Media.Should().ContainSingle();
    }

    [Test]
    public void AddMedia_WhenAtLimit_ReturnsFailureResult()
    {
        // Arrange
        var complaint = CreateOpenComplaint();
        const int uploaderId = 10;

        for (var i = 1; i <= ComplaintConstants.MaxMediaAttachments; i++)
        {
            complaint.AddMedia(Media.Create(
                complaintId: 1,
                url: $"https://blob.acls.io/evidence/photo{i}.jpg",
                type: "image/jpeg",
                uploadedByUserId: uploaderId));
        }

        var extraMedia = Media.Create(
            complaintId: 1,
            url: "https://blob.acls.io/evidence/overflow.jpg",
            type: "image/jpeg",
            uploadedByUserId: uploaderId);

        // Act
        var result = complaint.AddMedia(extraMedia);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.MaxMediaAttachmentsExceeded");
        complaint.Media.Should().HaveCount(ComplaintConstants.MaxMediaAttachments);
    }
}
