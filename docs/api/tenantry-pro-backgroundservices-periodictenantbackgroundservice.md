# `PeriodicTenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that performs work for every tenant on a recurring interval. The first sweep runs at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md). Each tenant is processed inside its own scope with per-tenant failure isolation (see [`TenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-tenantbackgroundservice.md)); a failure in one sweep does not stop later sweeps.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) with the per-tenant work and [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) with the cadence, then register with `builder.Services.AddHostedService<MySweep>()`.

```csharp
public abstract class PeriodicTenantBackgroundService<TKey> : TenantBackgroundService<TKey>, IHostedService, IDisposable where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits `BackgroundService` → [`TenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-tenantbackgroundservice.md).

Implements `IHostedService`, `IDisposable`.

## Constructors

### `PeriodicTenantBackgroundService(ITenantScopeFactory<TKey>, ITenantStoreAccessor<TKey>, ILogger)`

Base class for a hosted service that performs work for every tenant on a recurring interval. The first sweep runs at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md). Each tenant is processed inside its own scope with per-tenant failure isolation (see [`TenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-tenantbackgroundservice.md)); a failure in one sweep does not stop later sweeps.

```csharp
protected PeriodicTenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantStoreAccessor<TKey> storeAccessor, ILogger logger)
```

Parameters:

- `scopeFactory` [`ITenantScopeFactory<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantscopefactory): Creates a scope per tenant for each run.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) with the per-tenant work and [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) with the cadence, then register with `builder.Services.AddHostedService<MySweep>()`.

## Properties

### `Interval`

How long to wait between sweeps. Must be greater than zero.

```csharp
protected abstract TimeSpan Interval { get; }
```

Value: `TimeSpan`

## Methods

### `ExecuteAsync(CancellationToken)`

This method is called when the `IHostedService` starts. The implementation should return a task that represents the lifetime of the long running operation(s) being performed.

```csharp
protected override Task ExecuteAsync(CancellationToken stoppingToken)
```

Parameters:

- `stoppingToken` `CancellationToken`: Triggered when `StopAsync(CancellationToken)` is called.

Returns: `Task`: A `Task` that represents the long running operations.

See [Worker Services in .NET](https://learn.microsoft.com/dotnet/core/extensions/workers) for implementation guidelines.
