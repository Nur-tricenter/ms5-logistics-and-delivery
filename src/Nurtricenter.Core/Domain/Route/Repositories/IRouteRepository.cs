namespace Nurtricenter.Core.Domain.Route.Repositories;

using Joseco.DDD.Core.Abstractions;
using Nurtricenter.Core.Domain.Route;

public interface IRouteRepository : IRepository<Route>
{
    Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Route?> GetLatestRouteForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<Route?> GetByCourierAndDateAsync(Guid courierId, DateOnly date, CancellationToken cancellationToken = default);
    Task UpdateAsync(Route route, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
