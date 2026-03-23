using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.Commands.ResolveComplaint;
using ACLS.Domain.Complaints;
using ACLS.Domain.Complaints.Events;
using ACLS.Domain.Staff;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace Application.Tests.Complaints;

[TestFixture]
public sealed class ResolveComplaintCommandHandlerTests
{
    private IComplaintRepository _complaintRepository = null!;
    private IStaffRepository _staffRepository = null!;
    private ICurrentPropertyContext _propertyContext = null!;
    private IPublisher _publisher = null!;
    private ResolveComplaintCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _complaintRepository = Substitute.For<IComplaintRepository>();
        _staffRepository     = Substitute.For<IStaffRepository>();
        _propertyContext     = Substitute.For<ICurrentPropertyContext>();
        _publisher           = Substitute.For<IPublisher>();

        _propertyContext.PropertyId.Returns(TestDataFactory.PropertyId);
        _propertyContext.UserId.Returns(TestDataFactory.StaffMemberId);

        _handler = new ResolveComplaintCommandHandler(
            _complaintRepository,
            _staffRepository,
            _propertyContext,
            _publisher);
    }

    // ─── 4.4.1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsInProgress_ReturnsSuccess()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.IN_PROGRESS,
            assignedStaffId: TestDataFactory.StaffMemberId);

        var staff = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(TestDataFactory.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ─── 4.4.2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsInProgress_SetsStatusToResolved()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.IN_PROGRESS,
            assignedStaffId: TestDataFactory.StaffMemberId);

        var staff = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(TestDataFactory.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("RESOLVED");
    }

    // ─── 4.4.3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsInProgress_SetsStaffAvailabilityToAvailable()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.IN_PROGRESS,
            assignedStaffId: TestDataFactory.StaffMemberId);

        var staff = TestDataFactory.CreateStaff(availability: StaffState.BUSY);

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(TestDataFactory.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        staff.Availability.Should().Be(StaffState.AVAILABLE);
    }

    // ─── 4.4.4 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsInProgress_SetsResolvedAt()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.IN_PROGRESS,
            assignedStaffId: TestDataFactory.StaffMemberId);

        var staff = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(TestDataFactory.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResolvedAt.Should().NotBeNull();
        result.Value.ResolvedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ─── 4.4.5 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsInProgress_PublishesComplaintResolvedEvent()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.IN_PROGRESS,
            assignedStaffId: TestDataFactory.StaffMemberId);

        var staff = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(TestDataFactory.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _publisher.Received(1).Publish(
            Arg.Any<ComplaintResolvedEvent>(),
            Arg.Any<CancellationToken>());
    }

    // ─── 4.4.6 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsOpen_ReturnsInvalidTransitionFailure()
    {
        // Arrange — Complaint is OPEN (not IN_PROGRESS), so Resolve() should fail
        var complaint = TestDataFactory.CreateComplaint(status: TicketStatus.OPEN);

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }

    // ─── 4.4.7 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintIsAlreadyResolved_ReturnsInvalidTransitionFailure()
    {
        // Arrange — Complaint is RESOLVED; calling Resolve() again should fail
        var complaint = TestDataFactory.CreateComplaint(
            status:          TicketStatus.RESOLVED,
            assignedStaffId: TestDataFactory.StaffMemberId);

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        var command = new ResolveComplaintCommand(complaint.ComplaintId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.InvalidStatusTransition");
    }
}
