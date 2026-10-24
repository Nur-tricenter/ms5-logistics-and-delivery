namespace Nurtricenter.Test.Routes.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Routes.Start;

public class StartRouteValidatorTests
{
    private readonly StartRouteValidator _validator = new();

    [Fact]
    public void Validate_WithValidRouteId_IsValid()
    {
        var result = _validator.Validate(new StartRouteCommand(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyRouteId_IsInvalid()
    {
        var result = _validator.Validate(new StartRouteCommand(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyRouteId_ReturnsErrorForRouteIdProperty()
    {
        var result = _validator.Validate(new StartRouteCommand(Guid.Empty));
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(StartRouteCommand.RouteId));
    }
}
