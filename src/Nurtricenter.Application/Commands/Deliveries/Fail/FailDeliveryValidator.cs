namespace Nurtricenter.Application.Commands.Deliveries.Fail;

using FluentValidation;

public sealed class FailDeliveryValidator : AbstractValidator<FailDeliveryCommand>
{
	public FailDeliveryValidator()
	{
		RuleFor(x => x.RouteId).NotEmpty();
		RuleFor(x => x.DeliveryId).NotEmpty();
		RuleFor(x => x.Reason).NotEmpty();
	}
}
