namespace Nurtricenter.Test.Domain.ValueObjects;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;

public class DeliveryAddressTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsDescriptionCorrectly()
    {
        var coord = new Coordinate(10.0, 20.0);
        var address = new DeliveryAddress("123 Main St", coord);
        address.Description.Should().Be("123 Main St");
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsPlanarCoordinateCorrectly()
    {
        var coord = new Coordinate(10.0, 20.0);
        var address = new DeliveryAddress("123 Main St", coord);
        address.PlanarCoordinate.Should().Be(coord);
    }

    [Fact]
    public void Constructor_WithEmptyDescription_ThrowsDomainExceptionWithEmptyDescriptionCode()
    {
        var coord = new Coordinate(10.0, 20.0);
        var act = () => new DeliveryAddress("", coord);
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryAddress.EmptyDescription");
    }

    [Fact]
    public void Constructor_WithWhiteSpaceDescription_ThrowsDomainException()
    {
        var coord = new Coordinate(10.0, 20.0);
        var act = () => new DeliveryAddress("   ", coord);
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryAddress.EmptyDescription");
    }

    [Fact]
    public void Constructor_WithNullCoordinate_ThrowsDomainExceptionWithNullCoordinateCode()
    {
        var act = () => new DeliveryAddress("123 Main St", null!);
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryAddress.NullCoordinate");
    }
}
