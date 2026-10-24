namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Deliveries.Fail;

public class FailDeliveryValidatorTests
{
    private readonly FailDeliveryValidator _validator = new();

    private static FailDeliveryCommand ValidCommand()
        => new(Guid.NewGuid(), Guid.NewGuid(), "Customer was not home.");

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
    public void Validate_WithEmptyReason_IsInvalid()
        => _validator.Validate(ValidCommand() with { Reason = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_WithAllFieldsEmpty_ReturnsThreeErrors()
    {
        var command = new FailDeliveryCommand(Guid.Empty, Guid.Empty, "");
        _validator.Validate(command).Errors.Should().HaveCount(3);
    }
}
