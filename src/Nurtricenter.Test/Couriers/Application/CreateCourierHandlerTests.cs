namespace Nurtricenter.Test.Couriers.Application;

using FluentAssertions;
using Joseco.DDD.Core.Abstractions;
using Moq;
using Nurtricenter.Application.Commands.Couriers.Create;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Repositories;

public class CreateCourierHandlerTests
{
	private readonly Mock<ICourierRepository> _repositoryMock;
	private readonly Mock<IUnitOfWork> _unitOfWorkMock;
	private readonly CreateCourierHandler _handler;

	public CreateCourierHandlerTests()
	{
		_repositoryMock = new Mock<ICourierRepository>();
		_unitOfWorkMock = new Mock<IUnitOfWork>();

		_handler = new CreateCourierHandler(
			_repositoryMock.Object,
			_unitOfWorkMock.Object);
	}

	[Fact]
	public async Task Handle_WithValidCommand_ReturnsResponseWithMatchingFullName()
	{
		// Arrange
		const string fullName = "Jane Smith";
		var command = new CreateCourierCommand(fullName);

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.FullName.Should().Be(fullName);
	}

	[Fact]
	public async Task Handle_WithValidCommand_ReturnsResponseWithAvailableStatus()
	{
		// Arrange
		var command = new CreateCourierCommand("Jane Smith");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Status.Should().Be(CourierStatus.Available);
	}

	[Fact]
	public async Task Handle_WithValidCommand_ReturnsResponseWithNonEmptyId()
	{
		// Arrange
		var command = new CreateCourierCommand("Jane Smith");

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Id.Should().NotBeEmpty();
	}
}
