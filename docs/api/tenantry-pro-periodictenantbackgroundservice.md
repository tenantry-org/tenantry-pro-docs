# `PeriodicTenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that performs work for every tenant on a recurring interval. The first sweep runs at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md). Each tenant is processed inside its own scope with per-tenant failure isolation (see [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md)). A sweep that fails as a whole, because the tenant store cannot be read, say, is logged, and the next sweep runs on schedule: the service keeps running, and does not stop the host.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work and [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md) with the cadence, then register with `builder.Services.AddHostedService<MySweep>()`.

```csharp
public abstract class PeriodicTenantBackgroundService<TKey> : TenantBackgroundService<TKey>, IHostedService, IDisposable where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits `BackgroundService` → [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md).

Implements `IHostedService`, `IDisposable`.

## Constructors

### `PeriodicTenantBackgroundService(ITenantScopeFactory<TKey>, ITenantLookup<TKey>, ILogger)`

Base class for a hosted service that performs work for every tenant on a recurring interval. The first sweep runs at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md). Each tenant is processed inside its own scope with per-tenant failure isolation (see [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md)). A sweep that fails as a whole, because the tenant store cannot be read, say, is logged, and the next sweep runs on schedule: the service keeps running, and does not stop the host.

```csharp
protected PeriodicTenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantLookup<TKey> tenantLookup, ILogger logger)
```

Parameters:

- `scopeFactory` `ITenantScopeFactory<TKey>`: Creates a scope per tenant for each run.
- `tenantLookup` `ITenantLookup<TKey>`: Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants, and failed sweeps; also [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md).

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work and [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md) with the cadence, then register with `builder.Services.AddHostedService<MySweep>()`.

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
