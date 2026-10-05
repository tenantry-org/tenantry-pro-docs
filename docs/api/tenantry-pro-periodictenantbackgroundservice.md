# `PeriodicTenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that does work for every active tenant at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md).

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work and     [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md) with the cadence, then register with     `builder.Services.AddHostedService<MySweep>()`.

Each sweep runs the tenants as [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md) does: each in its own scope,     [`TenantBackgroundService<TKey>.MaxConcurrency`](tenantry-pro-tenantbackgroundservice.md) at a time, and a tenant that fails is logged     while the others go on. A sweep that fails as a whole, because the tenant store cannot be read or     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) threw, is logged, and the next sweep runs on     schedule, so the service does not stop the host. A sweep that     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) skips is not made up: the next one runs at the     next [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md).

Every instance of the application runs its own sweeps. Override     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) to run each sweep on one instance, or use     Hangfire's `AddOrUpdateForEachTenant` or Quartz.NET's `ForEachTenant()` with a clustered job store.

```csharp
public abstract class PeriodicTenantBackgroundService<TKey> : TenantBackgroundService<TKey>, IHostedService, IDisposable where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits `BackgroundService` → [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md).

Implements `IHostedService`, `IDisposable`.

## Constructors

### `PeriodicTenantBackgroundService(ITenantScopeFactory<TKey>, ITenantLookup<TKey>, ILogger, TimeProvider?)`

Base class for a hosted service that does work for every active tenant at startup, then again every [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md).

```csharp
protected PeriodicTenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantLookup<TKey> tenantLookup, ILogger logger, TimeProvider? timeProvider = null)
```

Parameters:

- `scopeFactory` `ITenantScopeFactory<TKey>`: Creates a scope per tenant for each run.
- `tenantLookup` `ITenantLookup<TKey>`: Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants, and failed sweeps; also [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md).
- `timeProvider` `TimeProvider`: Times the sweeps; `System` when it is null.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work and     [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md) with the cadence, then register with     `builder.Services.AddHostedService<MySweep>()`.

Each sweep runs the tenants as [`TenantBackgroundService<TKey>`](tenantry-pro-tenantbackgroundservice.md) does: each in its own scope,     [`TenantBackgroundService<TKey>.MaxConcurrency`](tenantry-pro-tenantbackgroundservice.md) at a time, and a tenant that fails is logged     while the others go on. A sweep that fails as a whole, because the tenant store cannot be read or     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) threw, is logged, and the next sweep runs on     schedule, so the service does not stop the host. A sweep that     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) skips is not made up: the next one runs at the     next [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md).

Every instance of the application runs its own sweeps. Override     [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) to run each sweep on one instance, or use     Hangfire's `AddOrUpdateForEachTenant` or Quartz.NET's `ForEachTenant()` with a clustered job store.

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
