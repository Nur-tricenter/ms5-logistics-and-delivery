namespace Nurtricenter.Test.Routes.Application;

using FluentAssertions;
using Nurtricenter.Application.Commands.Routes.Create;

public class CreateRouteValidatorTests
{
    private readonly CreateRouteValidator _validator = new();

    // ──────────────────────────────────────────────────────────────────────
    // CourierId rules
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidCourierIdAndFutureDate_IsValid()
    {
        // Arrange
        var command = new CreateRouteCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyCourierId_IsInvalid()
    {
        // Arrange
        var command = new CreateRouteCommand(Guid.Empty, DateOnly.FromDateTime(DateTime.Today));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyCourierId_ReturnsErrorForCourierIdProperty()
    {
        // Arrange
        var command = new CreateRouteCommand(Guid.Empty, DateOnly.FromDateTime(DateTime.Today));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateRouteCommand.CourierId));
    }

    // ──────────────────────────────────────────────────────────────────────
    // ScheduledDate rules
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithScheduledDateEqualToToday_IsValid()
    {
        // Arrange
        var command = new CreateRouteCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithScheduledDateInFuture_IsValid()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var command = new CreateRouteCommand(Guid.NewGuid(), futureDate);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithScheduledDateInPast_IsInvalid()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var command = new CreateRouteCommand(Guid.NewGuid(), yesterday);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithScheduledDateInPast_ReturnsExpectedErrorMessage()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var command = new CreateRouteCommand(Guid.NewGuid(), yesterday);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateRouteCommand.ScheduledDate) &&
            e.ErrorMessage == "Scheduled date cannot be in the past.");
    }

    [Fact]
    public void Validate_WithScheduledDateInPast_ReturnsErrorForScheduledDateProperty()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var command = new CreateRouteCommand(Guid.NewGuid(), yesterday);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateRouteCommand.ScheduledDate));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Combined failure
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WithEmptyCourierIdAndPastDate_ReturnsTwoErrors()
    {
        // Arrange
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var command = new CreateRouteCommand(Guid.Empty, yesterday);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.Errors.Should().HaveCount(2);
    }
}
