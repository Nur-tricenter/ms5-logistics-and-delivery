using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore.Persistence;
using Joseco.Outbox.EFCore.Procesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nurtricenter.Core.Domain.Courier.Repositories;
using Nurtricenter.Core.Domain.Route.Repositories;
using Nurtricenter.Core.Interfaces.Services.ClinicService;
using Nurtricenter.Core.Options;
using Nurtricenter.Infrastructure.Data;
using Nurtricenter.Infrastructure.Data.Repositories;
using Nurtricenter.Infrastructure.Observability;
using Nurtricenter.Infrastructure.Options;
using Nurtricenter.Infrastructure.Services;

namespace Nurtricenter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<ICourierRepository,
                              CourierRepository>();
        services.AddScoped<IRouteRepository,
                              RouteRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Transactional outbox: the unit of work exposes the outbox table of the same context,
        // so outbox rows are persisted in the same transaction as the aggregates.
        services.AddScoped<IOutboxDatabase<DomainEvent>, UnitOfWork>();
        AddOutboxServices<DomainEvent>(services);

        services.Configure<ClinicServiceOptions>(
            configuration.GetSection(ClinicServiceOptions.SectionName));

        services.Configure<BranchCoordinatesOptions>(
            configuration.GetSection(BranchCoordinatesOptions.SectionName));

        // External services

        services.AddHttpClient<IClinicService, ClinicService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ClinicServiceOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
        });

        return services;
    }

    /// <summary>
    /// Registers the transactional outbox services for <typeparamref name="TEvent"/>.
    /// This is the manual equivalent of <c>Joseco.Outbox.EFCore.DependencyInjection.AddOutbox&lt;TEvent&gt;()</c>,
    /// which cannot be called directly: that method is compiled against MediatR 12 and calls the
    /// removed <c>ServiceCollectionExtensions.AddMediatR</c>, so it throws a <c>TypeLoadException</c>
    /// on MediatR 14.
    /// </summary>
    private static void AddOutboxServices<TEvent>(IServiceCollection services)
    {
        services.AddScoped<EfCoreOutboxService<TEvent>>();
        services.AddScoped<IOutboxService<TEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<TEvent>>());
        services.AddScoped<IOutboxRepository<TEvent>>(sp => sp.GetRequiredService<EfCoreOutboxService<TEvent>>());
        services.AddScoped<OutboxProcessor<TEvent>>();

        // Wraps the registration above so every staged outbox row inherits the ambient trace.
        // Must run AFTER the AddScoped calls, or Scrutor decorates nothing.
        services.Decorate<IOutboxService<TEvent>, OutboxTracingService<TEvent>>();
    }
}
