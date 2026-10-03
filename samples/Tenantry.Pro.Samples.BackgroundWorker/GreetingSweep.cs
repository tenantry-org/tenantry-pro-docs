using Tenantry;
using Tenantry.Pro;

namespace Tenantry.Pro.Samples.BackgroundWorker;

/// <summary>
/// Recurring work for every tenant. <see cref="PeriodicTenantBackgroundService{TKey}"/> sweeps the tenants at startup
/// and then every <see cref="Interval"/>, calling <see cref="ExecuteForTenantAsync"/> once for each, inside that
/// tenant's scope. A tenant whose work throws is logged and does not stop the others, and a sweep that cannot read
/// the store is logged and tried again at the next interval, so neither stops the host.
/// </summary>
public sealed class GreetingSweep(
    ITenantScopeFactory<string> scopeFactory,
    ITenantLookup<string> tenants,
    SweepSettings settings,
    ILogger<GreetingSweep> logger)
    : PeriodicTenantBackgroundService<string>(scopeFactory, tenants, logger)
{
    protected override TimeSpan Interval => settings.Interval;

    protected override Task ExecuteForTenantAsync(ITenantScope<string> scope, CancellationToken cancellationToken)
    {
        // The sweep runs for every tenant in the store, suspended ones included: the work checks the status.
        if (scope.Tenant is not Tenant { IsActive: true })
        {
            Logger.LogInformation("Skipping tenant '{TenantId}': it is not active", scope.Tenant.TenantId);
            return Task.CompletedTask;
        }

        // Scoped services resolved from the tenant's scope see that tenant.
        var greeter = scope.ServiceProvider.GetRequiredService<GreetingService>();
        Logger.LogInformation("{Greeting}", greeter.Greeting());
        return Task.CompletedTask;
    }
}

/// <summary>How often <see cref="GreetingSweep"/> runs.</summary>
public sealed record SweepSettings(TimeSpan Interval);
