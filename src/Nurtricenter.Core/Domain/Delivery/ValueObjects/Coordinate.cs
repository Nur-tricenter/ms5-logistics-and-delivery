namespace Nurtricenter.Core.Domain.Delivery.ValueObjects;

public sealed record Coordinate
{
	public double Latitude { get; }
	public double Longitude { get; }

	public Coordinate(double latitude, double longitude)
	{
		Latitude = latitude;
		Longitude = longitude;
	}

	private Coordinate() { }

	public double DistanceTo(Coordinate other)
	{
		const double earthRadiusKm = 6371;

		var dLat = ToRadians(other.Latitude - Latitude);
		var dLon = ToRadians(other.Longitude - Longitude);

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude)) *
				Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

		return earthRadiusKm * c;
	}

	private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

	public override string ToString() => $"({Latitude}, {Longitude})";
}
