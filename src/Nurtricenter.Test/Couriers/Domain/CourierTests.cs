namespace Nurtricenter.Test.Couriers.Domain;

using FluentAssertions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Events;

public class CourierTests
{
	[Fact]
	public void Constructor_WithValidFullName_SetsFullNameCorrectly()
	{
		// Arrange
		var id = Guid.NewGuid();
		const string fullName = "John Doe";

		// Act
		var courier = new Courier(id, fullName);

		// Assert
		courier.FullName.Should().Be(fullName);
	}

	[Fact]
	public void Constructor_WithValidFullName_SetsDefaultStatusToAvailable()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var courier = new Courier(id, "John Doe");

		// Assert
		courier.Status.Should().Be(CourierStatus.Available);
	}

	[Fact]
	public void Constructor_WithValidFullName_RaisesCourierCreatedEvent()
	{
		// Arrange
		var id = Guid.NewGuid();
		const string fullName = "John Doe";

		// Act
		var courier = new Courier(id, fullName);

		// Assert
		courier.DomainEvents.Should().ContainSingle()
			.Which.Should().BeOfType<CourierCreatedEvent>();
	}

	[Fact]
	public void Constructor_WithValidFullName_RaisedEventContainsCorrectData()
	{
		// Arrange
		var id = Guid.NewGuid();
		const string fullName = "John Doe";

		// Act
		var courier = new Courier(id, fullName);

		// Assert
		var evt = courier.DomainEvents.OfType<CourierCreatedEvent>().Single();
		evt.CourierId.Should().Be(id);
		evt.FullName.Should().Be(fullName);
		evt.Status.Should().Be(CourierStatus.Available);
	}

	[Fact]
	public void Constructor_WithEmptyFullName_ThrowsDomainException()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var act = () => new Courier(id, string.Empty);

		// Assert
		act.Should().Throw<DomainException>();
	}

	[Fact]
	public void Constructor_WithWhiteSpaceFullName_ThrowsDomainException()
	{
		// Arrange
		var id = Guid.NewGuid();

		// Act
		var act = () => new Courier(id, "   ");

		// Assert
		act.Should().Throw<DomainException>();
	}

	[Fact]
	public void SetStatus_ToDifferentStatus_UpdatesStatus()
	{
		// Arrange
		var courier = new Courier(Guid.NewGuid(), "John Doe");

		// Act
		courier.SetStatus(CourierStatus.OnRoute);

		// Assert
		courier.Status.Should().Be(CourierStatus.OnRoute);
	}

	[Fact]
	public void SetStatus_ToDifferentStatus_RaisesCourierStatusChangedEvent()
	{
		// Arrange
		var courier = new Courier(Guid.NewGuid(), "John Doe");

		// Act
		courier.SetStatus(CourierStatus.OnRoute);

		// Assert
		courier.DomainEvents.Should().ContainSingle(e => e is CourierStatusChangedEvent);
	}

	[Fact]
	public void SetStatus_ToDifferentStatus_RaisedEventContainsCorrectOldAndNewStatus()
	{
		// Arrange
		var courier = new Courier(Guid.NewGuid(), "John Doe");

		// Act
		courier.SetStatus(CourierStatus.OnRoute);

		// Assert
		var evt = courier.DomainEvents.OfType<CourierStatusChangedEvent>().Single();
		evt.OldStatus.Should().Be(CourierStatus.Available);
		evt.NewStatus.Should().Be(CourierStatus.OnRoute);
	}

	[Fact]
	public void SetStatus_ToSameStatus_DoesNotRaiseAdditionalEvent()
	{
		// Arrange
		var courier = new Courier(Guid.NewGuid(), "John Doe");
		var eventCountAfterCreation = courier.DomainEvents.Count;

		// Act
		courier.SetStatus(CourierStatus.Available); // same as default

		// Assert
		courier.DomainEvents.Count.Should().Be(eventCountAfterCreation);
	}
}
