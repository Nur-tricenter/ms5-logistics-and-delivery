#pragma warning disable CS8618 // Parameterless constructor for serialization

namespace Nurtricenter.Core.Domain.Delivery.ValueObjects;

using Joseco.DDD.Core.Results;

public sealed record DeliveryAddress
{
	public string Description { get; }
	public Coordinate PlanarCoordinate { get; }

	public DeliveryAddress(string description, Coordinate planarCoordinate)
	{
		if (string.IsNullOrWhiteSpace(description))
			throw new DomainException(
				new Error(
					"DeliveryAddress.EmptyDescription",
					"Description cannot be empty.",
					ErrorType.Validation));

		if (planarCoordinate is null)
			throw new DomainException(
				new Error(
					"DeliveryAddress.NullCoordinate",
					"Planar coordinate is required.",
					ErrorType.Validation));

		Description = description;
		PlanarCoordinate = planarCoordinate;
	}

	private DeliveryAddress() { }
}
