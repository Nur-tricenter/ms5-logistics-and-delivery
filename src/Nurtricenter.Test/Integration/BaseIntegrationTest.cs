namespace Nurtricenter.Test.Integration;

using Microsoft.Extensions.DependencyInjection;
using Nurtricenter.Infrastructure.Data;

[Collection("Integration")]
public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>
{
    protected readonly IntegrationTestWebAppFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task ExecuteInScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }
    
    protected async Task<T> ExecuteInScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        using var scope = Factory.Services.CreateScope();
        return await action(scope.ServiceProvider);
    }

    protected async Task ClearDatabaseAsync()
    {
        await ExecuteInScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<AppDbContext>();
            db.Routes.RemoveRange(db.Routes);
            db.Couriers.RemoveRange(db.Couriers);
            await db.SaveChangesAsync();
        });
    }
}
