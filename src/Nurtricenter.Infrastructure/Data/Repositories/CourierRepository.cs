namespace Nurtricenter.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Nurtricenter.Core.Domain.Courier;
using Nurtricenter.Core.Domain.Courier.Enums;
using Nurtricenter.Core.Domain.Courier.Repositories;

public sealed class CourierRepository : ICourierRepository
{
	private readonly AppDbContext _context;

	public CourierRepository(AppDbContext context)
	{
		_context = context;
	}

	public async Task<Courier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
		=> await _context.Couriers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

	public async Task<IReadOnlyList<Courier>> GetAllAsync(CancellationToken cancellationToken = default)
		=> await _context.Couriers.AsNoTracking().ToListAsync(cancellationToken);

	public async Task<IReadOnlyList<Courier>> GetByStatusAsync(CourierStatus status, CancellationToken cancellationToken = default)
		=> await _context.Couriers.Where(c => c.Status == status).AsNoTracking().ToListAsync(cancellationToken);

	public async Task AddAsync(Courier courier, CancellationToken cancellationToken = default)
		=> await _context.Couriers.AddAsync(courier, cancellationToken);

	public Task UpdateAsync(Courier courier, CancellationToken cancellationToken = default)
	{
		_context.Couriers.Update(courier);
		return Task.CompletedTask;
	}

	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		var courier = await GetByIdAsync(id, cancellationToken);
		if (courier is not null)
			_context.Couriers.Remove(courier);
	}
}
