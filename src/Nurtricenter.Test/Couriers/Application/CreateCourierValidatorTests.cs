namespace Nurtricenter.Test.Couriers.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Couriers.Create;

public class CreateCourierValidatorTests
{
    private readonly CreateCourierValidator _validator = new();

    [Fact]
    public void Validate_WithValidFullName_IsValid()
    {
        var result = _validator.Validate(new CreateCourierCommand("John Doe"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyFullName_IsInvalid()
    {
        var result = _validator.Validate(new CreateCourierCommand(""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourierCommand.FullName));
    }

    [Fact]
    public void Validate_WithWhitespaceFullName_IsInvalid()
    {
        var result = _validator.Validate(new CreateCourierCommand("   "));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourierCommand.FullName));
    }

    [Fact]
    public void Validate_WithFullNameExactly200Chars_IsValid()
    {
        var result = _validator.Validate(new CreateCourierCommand(new string('a', 200)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithFullName201Chars_IsInvalid()
    {
        var result = _validator.Validate(new CreateCourierCommand(new string('a', 201)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCourierCommand.FullName));
    }
}
