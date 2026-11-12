namespace Nurtricenter.Application.Commands.Routes.Create;

using FluentValidation;

public sealed class CreateRouteValidator : AbstractValidator<CreateRouteCommand>
{
	public CreateRouteValidator()
	{
		RuleFor(x => x.CourierId)
			.NotEmpty();

		RuleFor(x => x.ScheduledDate)
			.Must(date => date >= DateOnly.FromDateTime(DateTime.Today))
			.WithMessage("Scheduled date cannot be in the past.");
	}
}
