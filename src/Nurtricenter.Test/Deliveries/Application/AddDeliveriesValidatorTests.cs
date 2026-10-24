namespace Nurtricenter.Test.Deliveries.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Deliveries.Add;

public class AddDeliveriesValidatorTests
{
    private readonly AddDeliveriesValidator _validator = new();

    private static AddDeliveryItem ValidItem(string patientId = "PAT-001", string packageId = "PKG-001")
        => new(patientId, packageId, "123 Main St", 1.0, 2.0, "details");

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { ValidItem() }));
        result.IsValid.Should().BeTrue();
    }

    // Items collection rules

    [Fact]
    public void Validate_WithEmptyItemsList_IsInvalid()
    {
        var result = _validator.Validate(new AddDeliveriesCommand(Array.Empty<AddDeliveryItem>()));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyItemsList_ReturnsAtLeastOneDeliveryMessage()
    {
        var result = _validator.Validate(new AddDeliveriesCommand(Array.Empty<AddDeliveryItem>()));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "At least one delivery item is required.");
    }

    // PatientId rules

    [Fact]
    public void Validate_WithEmptyPatientId_IsInvalid()
    {
        var item = ValidItem(patientId: "");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyPatientId_ReturnsPatientIdRequiredMessage()
    {
        var item = ValidItem(patientId: "");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Patient ID is required.");
    }

    // PackageId rules

    [Fact]
    public void Validate_WithEmptyPackageId_IsInvalid()
    {
        var item = ValidItem(packageId: "");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyPackageId_ReturnsPackageIdRequiredMessage()
    {
        var item = ValidItem(packageId: "");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Package ID is required.");
    }

    // Address rules

    [Fact]
    public void Validate_WithEmptyAddress_IsInvalid()
    {
        var item = new AddDeliveryItem("PAT-001", "PKG-001", "", 1.0, 2.0, "details");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyAddress_ReturnsAddressRequiredMessage()
    {
        var item = new AddDeliveryItem("PAT-001", "PKG-001", "", 1.0, 2.0, "details");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Address is required.");
    }

    // Combined failures on one item

    [Fact]
    public void Validate_WithAllFieldsEmptyOnOneItem_ReturnsThreeErrors()
    {
        var item = new AddDeliveryItem("", "", "", 0.0, 0.0, "");
        var result = _validator.Validate(new AddDeliveriesCommand(new[] { item }));
        result.Errors.Should().HaveCount(3); // PatientId, PackageId, Address
    }
}
