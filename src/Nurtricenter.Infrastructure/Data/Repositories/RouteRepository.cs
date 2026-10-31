namespace Nurtricenter.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Nurtricenter.Core.Domain.Route;
using Nurtricenter.Core.Domain.Route.Repositories;

public sealed class RouteRepository : IRouteRepository
{
    private readonly AppDbContext _context;

    public RouteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Route?> GetByIdAsync(Guid id, bool readOnly = false)
    {
        var query = _context.Routes.Include(r => r.Deliveries);

        return readOnly
            ? await query.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id)
            : await query.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(Route route)
        => await _context.Routes.AddAsync(route);

    public async Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Routes.Include(r => r.Deliveries).AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Route?> GetLatestRouteForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
        => await _context.Routes
            .Include(r => r.Deliveries)
            .Where(r => r.ScheduledDate == date)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Route?> GetByCourierAndDateAsync(Guid courierId, DateOnly date, CancellationToken cancellationToken = default)
        => await _context.Routes
            .Include(r => r.Deliveries)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CourierId == courierId && r.ScheduledDate == date, cancellationToken);

    public async Task<IReadOnlyList<Route>> GetByDeliveryPackageAsync(
        string? packageId, int? deliveryDayId, CancellationToken cancellationToken = default)
        => await _context.Routes
            .Include(r => r.Deliveries)
            .AsNoTracking()
            .Where(r => r.Deliveries.Any(d =>
                (packageId == null || d.Package.PackageId == packageId) &&
                (deliveryDayId == null || d.Package.DeliveryDayId == deliveryDayId)))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task UpdateAsync(Route route, CancellationToken cancellationToken = default)
    {
        _context.Routes.Update(route);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var route = await GetByIdAsync(id, readOnly: false);
        if (route is not null)
            _context.Routes.Remove(route);
    }
}
