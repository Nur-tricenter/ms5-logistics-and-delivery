namespace Nurtricenter.Test.Domain.ValueObjects;

using FluentAssertions;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;

public class CoordinateTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsLatitudeCorrectly()
    {
        var coord = new Coordinate(10.5, 20.5);
        coord.Latitude.Should().Be(10.5);
    }

    [Fact]
    public void Constructor_WithValidValues_SetsLongitudeCorrectly()
    {
        var coord = new Coordinate(10.5, 20.5);
        coord.Longitude.Should().Be(20.5);
    }

    [Fact]
    public void DistanceTo_SamePoint_ReturnsZero()
    {
        var coord = new Coordinate(10.0, 20.0);
        coord.DistanceTo(coord).Should().Be(0);
    }

    [Fact]
    public void DistanceTo_IsSymmetric()
    {
        var coord1 = new Coordinate(10.0, 20.0);
        var coord2 = new Coordinate(30.0, 40.0);
        
        var d1 = coord1.DistanceTo(coord2);
        var d2 = coord2.DistanceTo(coord1);
        
        d1.Should().BeApproximately(d2, 0.001);
    }

    [Fact]
    public void DistanceTo_KnownPairMadridToBarcelona_ReturnsApproximatelyCorrectKm()
    {
        var madrid = new Coordinate(40.4168, -3.7038);
        var barcelona = new Coordinate(41.3851, 2.1734);
        
        var distance = madrid.DistanceTo(barcelona);
        
        distance.Should().BeInRange(490, 520);
    }

    [Fact]
    public void DistanceTo_KnownPairEquatorPoints_ReturnsApproximatelyCorrectKm()
    {
        var point1 = new Coordinate(0.0, 0.0);
        var point2 = new Coordinate(0.0, 1.0);
        
        var distance = point1.DistanceTo(point2);
        
        distance.Should().BeInRange(110, 113);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var coord = new Coordinate(10.5, 20.3);
        coord.ToString().Should().Be("(10.5, 20.3)");
    }
}
