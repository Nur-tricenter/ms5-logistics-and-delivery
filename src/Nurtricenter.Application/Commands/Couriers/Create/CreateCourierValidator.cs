namespace Nurtricenter.Application.Commands.Couriers.Create;

using FluentValidation;

public sealed class CreateCourierValidator : AbstractValidator<CreateCourierCommand>
{
	public CreateCourierValidator()
	{
		RuleFor(x => x.FullName)
			.NotEmpty()
			.MaximumLength(200);
	}
}
