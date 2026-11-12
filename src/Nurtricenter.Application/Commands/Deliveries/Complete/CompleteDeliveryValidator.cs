namespace Nurtricenter.Application.Commands.Deliveries.Complete;

using FluentValidation;

public sealed class CompleteDeliveryValidator : AbstractValidator<CompleteDeliveryCommand>
{
	public CompleteDeliveryValidator()
	{
		RuleFor(x => x.RouteId).NotEmpty();
		RuleFor(x => x.DeliveryId).NotEmpty();
		RuleFor(x => x.EvidencePhotoUrl).NotEmpty();
		RuleFor(x => x.DigitalSignature).NotEmpty();
	}
}
