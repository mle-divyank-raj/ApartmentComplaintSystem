using ACLS.Application.Common.Interfaces;
using ACLS.Application.Complaints.Commands.SubmitComplaint;
using ACLS.Domain.Complaints;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace Application.Tests.Complaints;

[TestFixture]
public sealed class SubmitComplaintCommandHandlerTests
{
    private IComplaintRepository _complaintRepository = null!;
    private ICurrentPropertyContext _propertyContext = null!;
    private IPublisher _publisher = null!;
    private SubmitComplaintCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _complaintRepository = Substitute.For<IComplaintRepository>();
        _propertyContext     = Substitute.For<ICurrentPropertyContext>();
        _publisher           = Substitute.For<IPublisher>();

        _propertyContext.PropertyId.Returns(TestDataFactory.PropertyId);
        _propertyContext.UserId.Returns(TestDataFactory.ResidentId);

        // Simulate what EF Core does on SaveChanges — populate the identity key
        _complaintRepository
            .AddAsync(Arg.Any<Complaint>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                TestDataFactory.SetPrivateProperty(ci.Arg<Complaint>(), "ComplaintId", 1);
                return Task.CompletedTask;
            });

        _handler = new SubmitComplaintCommandHandler(
            _complaintRepository,
            _propertyContext,
            _publisher);
    }

    // ─── 4.5.1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_CreatesComplaintWithOpenStatus()
    {
        // Arrange
        var command = new SubmitComplaintCommand(
            Title:             "Leaking tap",
            Description:       "The kitchen tap is dripping.",
            Category:          "Plumbing",
            Urgency:           "MEDIUM",
            UnitId:            TestDataFactory.UnitId,
            PermissionToEnter: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("OPEN");
    }

    // ─── 4.5.2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithMediaUrls_CreatesMediaEntitiesWithUrlsOnly()
    {
        // Arrange
        var mediaUrls = new List<MediaUploadResult>
        {
            new("https://blob.example.com/photo1.jpg", "image/jpeg"),
            new("https://blob.example.com/photo2.png", "image/png")
        };

        var command = new SubmitComplaintCommand(
            Title:             "Broken window",
            Description:       "Window cracked.",
            Category:          "General",
            Urgency:           "HIGH",
            UnitId:            TestDataFactory.UnitId,
            PermissionToEnter: false,
            MediaUrls:         mediaUrls);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _complaintRepository.Received(2).AddMediaAsync(
            Arg.Is<Media>(m => m.Url.StartsWith("https://blob.example.com/")),
            Arg.Any<CancellationToken>());
    }

    // ─── 4.5.3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithMediaUrls_DoesNotCallStorageService()
    {
        // Arrange — No IStorageService is injected into the handler; this test
        // verifies the handler only receives already-uploaded URL strings.
        var command = new SubmitComplaintCommand(
            Title:             "Noise complaint",
            Description:       "Loud music late at night.",
            Category:          "Noise",
            Urgency:           "LOW",
            UnitId:            TestDataFactory.UnitId,
            PermissionToEnter: false,
            MediaUrls:
            [
                new MediaUploadResult("https://blob.example.com/audio.mp4", "video/mp4")
            ]);

        // Act — should complete without any storage calls (no IStorageService mock needed)
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // No IStorageService was injected → if the handler tried to use one, this test would not compile.
    }

    // ─── 4.5.4 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_SetsPropertyIdFromContext()
    {
        // Arrange
        var command = new SubmitComplaintCommand(
            Title:             "HVAC issue",
            Description:       "AC not cooling.",
            Category:          "HVAC",
            Urgency:           "MEDIUM",
            UnitId:            TestDataFactory.UnitId,
            PermissionToEnter: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PropertyId.Should().Be(TestDataFactory.PropertyId);
    }

    // ─── 4.5.5 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_RaisesComplaintSubmittedEvent()
    {
        // Arrange
        var command = new SubmitComplaintCommand(
            Title:             "Electrical fault",
            Description:       "Lights flickering.",
            Category:          "Electrical",
            Urgency:           "HIGH",
            UnitId:            TestDataFactory.UnitId,
            PermissionToEnter: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _publisher.Received(1).Publish(
            Arg.Any<ACLS.Domain.Complaints.Events.ComplaintSubmittedEvent>(),
            Arg.Any<CancellationToken>());
    }
}
