namespace Nurtricenter.Api.Endpoints.Deliveries;

public sealed record CompleteDeliveryBodyRequest(
	DateTime DeliveredAt,
	string EvidencePhotoUrl,
	string DigitalSignature);
