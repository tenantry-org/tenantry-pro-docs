using Tenantry.Core;

namespace BackgroundWorker;

/// <summary>
/// A non-HTTP background worker. There is no request to open a tenant scope, so the worker opens one
/// per tenant with <see cref="ITenantScopeFactory{TKey}"/>. Scoped services resolved from the scope's
/// provider see exactly that tenant.
/// </summary>
public sealed class TenantSweepWorker(
    ITenantScopeFactory<string> scopeFactory,
    ITenantStoreAccessor<string> tenants,
    IHostApplicationLifetime lifetime,
    ILogger<TenantSweepWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var tenant in await tenants.GetAllTenantsAsync(stoppingToken))
        {
            // One combined DI + tenant scope per tenant; disposing it restores the previous (empty) tenant.
            await using var scope = scopeFactory.CreateScope(tenant);

            var greeter = scope.ServiceProvider.GetRequiredService<GreetingService>();
            
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("{Greeting}", greeter.Greeting());
            }
        }

        // With only a tenant id (say, from a queue message), let the factory look the tenant up and run
        // the work inside its scope.
        await scopeFactory.RunInScopeAsync("globex", (scope, _) =>
        {
            logger.LogInformation("By id: {Greeting}", scope.ServiceProvider.GetRequiredService<GreetingService>().Greeting());
            return Task.CompletedTask;
        }, stoppingToken);

        logger.LogInformation("Sweep complete");
        lifetime.StopApplication();
    }
}
