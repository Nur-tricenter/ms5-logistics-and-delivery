namespace Nurtricenter.Test.Infrastructure.Messaging;

using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nur.Tricenter.Integration.Messages.Contracting;
using Nurtricenter.Application.Commands.Deliveries.Add;
using Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;
using Nurtricenter.Infrastructure.Messaging;

public class PackageValidatedConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PackageValidatedConsumer _consumer;
    private AddValidatedPackageCommand? _sent;

    public PackageValidatedConsumerTests()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<AddValidatedPackageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AddDeliveriesResponse>, CancellationToken>((command, _) => _sent = (AddValidatedPackageCommand)command)
            .ReturnsAsync(new AddDeliveriesResponse(Guid.NewGuid(), []));

        _consumer = new PackageValidatedConsumer(_mediatorMock.Object, NullLogger<PackageValidatedConsumer>.Instance);
    }

    private static PackageValidated MakeMessage(string trackingNumber = "NTR-A79C734C-20261024") => new("corr-1", "contracting")
    {
        PackageId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        PatientName = "Juan Perez",
        ProductionOrderId = Guid.NewGuid(),
        CatalogPlanId = Guid.NewGuid(),
        ReleasedAt = new DateTime(2026, 10, 28, 15, 0, 0, DateTimeKind.Utc),
        DeliveryDate = new DateOnly(2026, 10, 29),
        DeliveryAddress = "Av. Banzer 123, Santa Cruz",
        TrackingNumber = trackingNumber,
        Latitude = -17.78,
        Longitude = -63.18,
        DeliveryDayId = 7
    };

    [Fact]
    public async Task HandleAsync_MapsTheContractToTheCommand()
    {
        var message = MakeMessage();

        await _consumer.HandleAsync(message);

        _sent.Should().Be(new AddValidatedPackageCommand(
            message.PackageId.ToString(),
            message.PatientId.ToString(),
            message.PatientName,
            message.DeliveryDate,
            message.DeliveryAddress,
            message.Latitude,
            message.Longitude,
            message.TrackingNumber,
            message.DeliveryDayId));
    }

    [Fact]
    public async Task HandleAsync_WithoutDeliveryDate_UsesTheDayAfterRelease()
    {
        var message = MakeMessage() with { DeliveryDate = DateOnly.MinValue };

        await _consumer.HandleAsync(message);

        _sent!.DeliveryDate.Should().Be(new DateOnly(2026, 10, 29));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithoutPatientName_UsesThePatientId(string patientName)
    {
        var message = MakeMessage() with { PatientName = patientName };

        await _consumer.HandleAsync(message);

        _sent!.PatientName.Should().Be(message.PatientId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_WithoutTrackingNumber_UsesThePackageIdAsLabel(string trackingNumber)
    {
        var message = MakeMessage(trackingNumber);

        await _consumer.HandleAsync(message);

        _sent!.LabelData.Should().Be(message.PackageId.ToString());
    }

    [Fact]
    public async Task HandleAsync_WithNullCoordinates_PassesThemThroughForTheHandlerToResolve()
    {
        var message = MakeMessage() with { Latitude = null, Longitude = null };

        await _consumer.HandleAsync(message);

        _sent!.Latitude.Should().BeNull();
        _sent.Longitude.Should().BeNull();
    }
}
