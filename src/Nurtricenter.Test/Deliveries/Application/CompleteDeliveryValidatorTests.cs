namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Deliveries.Complete;

public class CompleteDeliveryValidatorTests
{
    private readonly CompleteDeliveryValidator _validator = new();

    private static CompleteDeliveryCommand ValidCommand() => new(
        Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow,
        "https://evidence.example.com/photo.jpg", "valid-sig");

    [Fact]
    public void Validate_WithValidCommand_IsValid()
        => _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_WithEmptyRouteId_IsInvalid()
        => _validator.Validate(ValidCommand() with { RouteId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_WithEmptyDeliveryId_IsInvalid()
        => _validator.Validate(ValidCommand() with { DeliveryId = Guid.Empty }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_WithEmptyEvidencePhotoUrl_IsInvalid()
        => _validator.Validate(ValidCommand() with { EvidencePhotoUrl = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_WithEmptyDigitalSignature_IsInvalid()
        => _validator.Validate(ValidCommand() with { DigitalSignature = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_WithAllFieldsEmpty_ReturnsFourErrors()
    {
        var command = new CompleteDeliveryCommand(Guid.Empty, Guid.Empty, DateTime.UtcNow, "", "");
        _validator.Validate(command).Errors.Should().HaveCount(4);
    }
}
