namespace Nurtricenter.Application.Commands.Deliveries.Add;

using FluentValidation;

public sealed class AddDeliveriesValidator : AbstractValidator<AddDeliveriesCommand>
{
    public AddDeliveriesValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one delivery item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.PatientId)
                .NotEmpty()
                .WithMessage("Patient ID is required.");

            item.RuleFor(x => x.PatientName)
                .NotEmpty()
                .WithMessage("Patient name is required.");

            item.RuleFor(x => x.PackageId)
                .NotEmpty()
                .WithMessage("Package ID is required.");

            item.RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required.");
        });
    }
}
