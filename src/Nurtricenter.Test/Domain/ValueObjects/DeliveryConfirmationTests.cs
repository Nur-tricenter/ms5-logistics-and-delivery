namespace Nurtricenter.Test.Domain.ValueObjects;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Delivery.ValueObjects;

public class DeliveryConfirmationTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsEvidencePhotoUrlCorrectly()
    {
        var confirmation = new DeliveryConfirmation(DateTime.UtcNow, "http://photo", "sig");
        confirmation.EvidencePhotoUrl.Should().Be("http://photo");
    }

    [Fact]
    public void Constructor_WithValidArguments_SetsDigitalSignatureCorrectly()
    {
        var confirmation = new DeliveryConfirmation(DateTime.UtcNow, "http://photo", "sig");
        confirmation.DigitalSignature.Should().Be("sig");
    }

    [Fact]
    public void Constructor_WithValidArguments_StoresDeliveredAtAsUtc()
    {
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var confirmation = new DeliveryConfirmation(localTime, "http://photo", "sig");
        confirmation.DeliveredAt.Should().Be(localTime.ToUniversalTime());
    }

    [Fact]
    public void Constructor_WithEmptyEvidencePhotoUrl_ThrowsDomainExceptionWithEmptyEvidenceUrlCode()
    {
        var act = () => new DeliveryConfirmation(DateTime.UtcNow, "", "sig");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryConfirmation.EmptyEvidenceUrl");
    }

    [Fact]
    public void Constructor_WithWhiteSpaceEvidencePhotoUrl_ThrowsDomainException()
    {
        var act = () => new DeliveryConfirmation(DateTime.UtcNow, "   ", "sig");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryConfirmation.EmptyEvidenceUrl");
    }

    [Fact]
    public void Constructor_WithEmptyDigitalSignature_ThrowsDomainExceptionWithEmptySignatureCode()
    {
        var act = () => new DeliveryConfirmation(DateTime.UtcNow, "http://photo", "");
        act.Should().Throw<DomainException>().Where(ex => ex.Error.Code == "DeliveryConfirmation.EmptySignature");
    }
}
