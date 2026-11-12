#pragma warning disable CS8618 // Parameterless constructor for serialization

namespace Nurtricenter.Core.Domain.Delivery.ValueObjects;

using Joseco.DDD.Core.Results;

public sealed record DeliveryConfirmation
{
	public DateTime DeliveredAt { get; }
	public string EvidencePhotoUrl { get; }
	public string DigitalSignature { get; }

	public DeliveryConfirmation(DateTime deliveredAt, string evidencePhotoUrl, string digitalSignature)
	{
		if (string.IsNullOrWhiteSpace(evidencePhotoUrl))
			throw new DomainException(
				new Error(
					"DeliveryConfirmation.EmptyEvidenceUrl",
					"Evidence photo URL cannot be empty.",
					ErrorType.Validation));

		if (string.IsNullOrWhiteSpace(digitalSignature))
			throw new DomainException(
				new Error(
					"DeliveryConfirmation.EmptySignature",
					"Digital signature cannot be empty.",
					ErrorType.Validation));

		DeliveredAt = deliveredAt.ToUniversalTime();
		EvidencePhotoUrl = evidencePhotoUrl;
		DigitalSignature = digitalSignature;
	}

	private DeliveryConfirmation() { }
}
