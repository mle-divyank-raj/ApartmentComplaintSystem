using ACLS.Application.Common.Interfaces;
using ACLS.Application.Outages.Commands.DeclareOutage;
using ACLS.Domain.Outages;
using ACLS.Domain.Outages.Events;
using FluentAssertions;
using MediatR;
using NSubstitute;
using NUnit.Framework;

namespace Application.Tests.Outages;

[TestFixture]
public sealed class DeclareOutageCommandHandlerTests
{
    private IOutageRepository _outageRepository = null!;
    private ICurrentPropertyContext _propertyContext = null!;
    private IPublisher _publisher = null!;
    private DeclareOutageCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _outageRepository = Substitute.For<IOutageRepository>();
        _propertyContext  = Substitute.For<ICurrentPropertyContext>();
        _publisher        = Substitute.For<IPublisher>();

        _propertyContext.PropertyId.Returns(TestDataFactory.PropertyId);
        _propertyContext.UserId.Returns(TestDataFactory.ManagerUserId);

        _handler = new DeclareOutageCommandHandler(
            _outageRepository,
            _propertyContext,
            _publisher);
    }

    // ─── 4.6.1 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_CreatesOutageRecord()
    {
        // Arrange
        var command = new DeclareOutageCommand(
            Title:       "Planned water shutdown",
            OutageType:  "Water",
            Description: "Pipe maintenance for 2 hours.",
            StartTime:   DateTime.UtcNow.AddHours(1),
            EndTime:     DateTime.UtcNow.AddHours(3));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _outageRepository.Received(1).AddAsync(
            Arg.Is<Outage>(o =>
                o.Title      == command.Title &&
                o.OutageType == OutageType.Water &&
                o.PropertyId == TestDataFactory.PropertyId),
            Arg.Any<CancellationToken>());
    }

    // ─── 4.6.2 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_PublishesOutageDeclaredEvent()
    {
        // Arrange
        var command = new DeclareOutageCommand(
            Title:       "Gas outage",
            OutageType:  "Gas",
            Description: "Emergency gas shutoff.",
            StartTime:   DateTime.UtcNow.AddMinutes(30),
            EndTime:     null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _publisher.Received(1).Publish(
            Arg.Any<OutageDeclaredEvent>(),
            Arg.Any<CancellationToken>());
    }

    // ─── 4.6.3 ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_WithValidInputs_DoesNotCallNotificationServiceDirectly()
    {
        // Arrange — INotificationService is NOT injected into this handler.
        // This test verifies the handler never calls the notification service inline.
        // Worker picks up OutageDeclaredEvent asynchronously instead.
        var command = new DeclareOutageCommand(
            Title:       "Electricity outage",
            OutageType:  "Electricity",
            Description: "Power works scheduled.",
            StartTime:   DateTime.UtcNow.AddHours(2),
            EndTime:     DateTime.UtcNow.AddHours(4));

        // Act — no INotificationService passed to handler; if handler tried to use one it would fail
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only one repository call and one event publish — no notification calls
        await _outageRepository.Received(1).AddAsync(Arg.Any<Outage>(), Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(Arg.Any<OutageDeclaredEvent>(), Arg.Any<CancellationToken>());
    }
}
