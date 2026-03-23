using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.Commands.AssignComplaint;
using ACLS.Domain.Complaints;
using ACLS.Domain.Complaints.Events;
using ACLS.Domain.Staff;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace Application.Tests.Complaints;

[TestFixture]
public sealed class AssignComplaintCommandHandlerTests
{
    private IComplaintRepository _complaintRepository = null!;
    private IStaffRepository _staffRepository = null!;
    private ICurrentPropertyContext _propertyContext = null!;
    private IPublisher _publisher = null!;
    private AssignComplaintCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _complaintRepository = Substitute.For<IComplaintRepository>();
        _staffRepository     = Substitute.For<IStaffRepository>();
        _propertyContext     = Substitute.For<ICurrentPropertyContext>();
        _publisher           = Substitute.For<IPublisher>();

        _propertyContext.PropertyId.Returns(TestDataFactory.PropertyId);
        _propertyContext.UserId.Returns(99);

        _handler = new AssignComplaintCommandHandler(
            _complaintRepository,
            _staffRepository,
            _propertyContext,
            _publisher);
    }

    // ─── 4.3.1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintAndStaffExist_ReturnsSuccess()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint();
        var staff     = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(staff.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new AssignComplaintCommand(complaint.ComplaintId, staff.StaffMemberId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // ─── 4.3.2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        _complaintRepository
            .GetByIdAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Complaint?)null);

        var command = new AssignComplaintCommand(999, TestDataFactory.StaffMemberId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.NotFound");
    }

    // ─── 4.3.3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenStaffNotFound_ReturnsNotFoundFailure()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((StaffMember?)null);

        var command = new AssignComplaintCommand(complaint.ComplaintId, 999);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StaffMember.NotFound");
    }

    // ─── 4.3.4 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintExists_SetsComplaintStatusToAssigned()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint();
        var staff     = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(staff.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new AssignComplaintCommand(complaint.ComplaintId, staff.StaffMemberId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("ASSIGNED");
    }

    // ─── 4.3.5 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintExists_SetsStaffAvailabilityToBusy()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint();
        var staff     = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(staff.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new AssignComplaintCommand(complaint.ComplaintId, staff.StaffMemberId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        staff.Availability.Should().Be(StaffState.BUSY);
    }

    // ─── 4.3.6 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintExists_PublishesDomainEvent()
    {
        // Arrange
        var complaint = TestDataFactory.CreateComplaint();
        var staff     = TestDataFactory.CreateStaff();

        _complaintRepository
            .GetByIdAsync(complaint.ComplaintId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(complaint);

        _staffRepository
            .GetByIdAsync(staff.StaffMemberId, TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns(staff);

        var command = new AssignComplaintCommand(complaint.ComplaintId, staff.StaffMemberId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _publisher.Received(1).Publish(
            Arg.Any<ComplaintAssignedEvent>(),
            Arg.Any<CancellationToken>());
    }

    // ─── 4.3.7 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WhenComplaintBelongsToDifferentProperty_ReturnsNotFound()
    {
        // Arrange — repository returns null for cross-property complaints (404 rule)
        _complaintRepository
            .GetByIdAsync(Arg.Any<int>(), TestDataFactory.PropertyId, Arg.Any<CancellationToken>())
            .Returns((Complaint?)null);

        var command = new AssignComplaintCommand(1, TestDataFactory.StaffMemberId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Complaint.NotFound");
    }
}
