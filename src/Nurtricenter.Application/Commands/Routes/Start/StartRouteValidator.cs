namespace Nurtricenter.Application.Commands.Routes.Start;

using FluentValidation;

public sealed class StartRouteValidator : AbstractValidator<StartRouteCommand>
{
	public StartRouteValidator()
	{
		RuleFor(x => x.RouteId).NotEmpty();
	}
}
