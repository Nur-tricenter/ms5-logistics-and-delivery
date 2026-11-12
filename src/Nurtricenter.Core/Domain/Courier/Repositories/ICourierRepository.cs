namespace Nurtricenter.Core.Domain.Courier.Repositories;

using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Enums;

public interface ICourierRepository
{
	Task<Courier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Courier>> GetAllAsync(CancellationToken cancellationToken = default);
	Task<IReadOnlyList<Courier>> GetByStatusAsync(CourierStatus status, CancellationToken cancellationToken = default);
	Task AddAsync(Courier courier, CancellationToken cancellationToken = default);
	Task UpdateAsync(Courier courier, CancellationToken cancellationToken = default);
	Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
