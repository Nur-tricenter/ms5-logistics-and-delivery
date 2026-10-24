namespace Nurtricenter.Test.Domain.ValueObjects;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;

public class ValidatedPackageTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsPackageIdCorrectly()
    {
        var package = new ValidatedPackage("PKG-123", "PAT-456", "LABEL-DATA");
        package.PackageId.Should().Be("PKG-123");
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsPatientIdCorrectly()
    {
        var package = new ValidatedPackage("PKG-123", "PAT-456", "LABEL-DATA");
        package.PatientId.Should().Be("PAT-456");
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsLabelDataCorrectly()
    {
        var package = new ValidatedPackage("PKG-123", "PAT-456", "LABEL-DATA");
        package.LabelData.Should().Be("LABEL-DATA");
    }

    [Fact]
    public void Constructor_WithEmptyPackageId_ThrowsDomainExceptionWithEmptyPackageIdCode()
    {
        var act = () => new ValidatedPackage("", "PAT-456", "LABEL-DATA");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "ValidatedPackage.EmptyPackageId");
    }

    [Fact]
    public void Constructor_WithWhiteSpacePackageId_ThrowsDomainException()
    {
        var act = () => new ValidatedPackage("   ", "PAT-456", "LABEL-DATA");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "ValidatedPackage.EmptyPackageId");
    }

    [Fact]
    public void Constructor_WithEmptyPatientId_ThrowsDomainExceptionWithEmptyPatientIdCode()
    {
        var act = () => new ValidatedPackage("PKG-123", "", "LABEL-DATA");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "ValidatedPackage.EmptyPatientId");
    }

    [Fact]
    public void Constructor_WithEmptyLabelData_ThrowsDomainExceptionWithEmptyLabelDataCode()
    {
        var act = () => new ValidatedPackage("PKG-123", "PAT-456", "");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "ValidatedPackage.EmptyLabelData");
    }
}
