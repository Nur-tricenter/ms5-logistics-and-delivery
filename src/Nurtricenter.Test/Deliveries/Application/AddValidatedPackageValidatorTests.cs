namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Deliveries.AddValidatedPackage;

public class AddValidatedPackageValidatorTests
{
    private readonly AddValidatedPackageValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new AddValidatedPackageCommand("PKG-001", "PAT-001", "Av. Banzer 123", null, null, "NTR-001");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllRequiredFieldsEmpty_ReturnsFourErrors()
    {
        var command = new AddValidatedPackageCommand("", "", "", null, null, "");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(
            nameof(AddValidatedPackageCommand.PackageId),
            nameof(AddValidatedPackageCommand.PatientId),
            nameof(AddValidatedPackageCommand.Address),
            nameof(AddValidatedPackageCommand.LabelData));
    }
}
