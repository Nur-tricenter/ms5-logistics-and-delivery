#pragma warning disable CS8618 // Private parameterless ctor for serialization

namespace Nurtricenter.Core.Domain.Courier;

using Joseco.DDD.Core.Abstractions;
using Joseco.DDD.Core.Results;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Events;

public sealed class Courier : Entity
{
	public string FullName { get; private set; }
	public CourierStatus Status { get; private set; }

	public Courier(Guid id, string fullName, CourierStatus status = CourierStatus.Available)
		: base(id)
	{
		if (string.IsNullOrWhiteSpace(fullName))
			throw new DomainException(
				new Error(
					"Courier.EmptyFullName",
					"Full name cannot be empty.",
					ErrorType.Validation));

		FullName = fullName;
		Status = status;

		AddDomainEvent(new CourierCreatedEvent(id, fullName, status));
	}

	private Courier() : base() { }

	public void SetStatus(CourierStatus status)
	{
		if (Status == status)
			return;

		var oldStatus = Status;
		Status = status;

		AddDomainEvent(new CourierStatusChangedEvent(Id, oldStatus, status));
	}
}
