namespace Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

using FluentValidation;

public sealed class AddValidatedPackageValidator : AbstractValidator<AddValidatedPackageCommand>
{
    public AddValidatedPackageValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty()
            .WithMessage("Package ID is required.");

        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("Patient ID is required.");

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Address is required.");

        RuleFor(x => x.LabelData)
            .NotEmpty()
            .WithMessage("Label data is required.");
    }
}
