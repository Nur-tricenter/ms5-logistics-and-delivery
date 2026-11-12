namespace Nurtricenter.Application.Commands.Couriers.Create;

using Joseco.DDD.Core.Abstractions;
using MediatR;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Repositories;

public sealed class CreateCourierHandler : IRequestHandler<CreateCourierCommand, CourierResponse>
{
	private readonly ICourierRepository _courierRepository;
	private readonly IUnitOfWork _unitOfWork;

	public CreateCourierHandler(ICourierRepository courierRepository, IUnitOfWork unitOfWork)
	{
		_courierRepository = courierRepository;
		_unitOfWork = unitOfWork;
	}

	public async Task<CourierResponse> Handle(CreateCourierCommand request, CancellationToken cancellationToken)
	{
		var courier = new Courier(Guid.NewGuid(), request.FullName);

		await _courierRepository.AddAsync(courier, cancellationToken);
		await _unitOfWork.CommitAsync(cancellationToken);

		return new CourierResponse(courier.Id, courier.FullName, courier.Status);
	}
}
