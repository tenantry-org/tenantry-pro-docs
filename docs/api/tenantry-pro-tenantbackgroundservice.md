# `TenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that does work for every active tenant once, then completes. Each tenant's work runs in its own scope, with a log scope with `TenantId` open on [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md). A tenant that fails is logged and the others go on; a tenant `ValidateTenantActivity` refuses is skipped.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your `DbContext`, repositories, …) from the scope's `ServiceProvider`, which is already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`. For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) instead.

```csharp
public abstract class TenantBackgroundService<TKey> : BackgroundService, IHostedService, IDisposable where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits `BackgroundService`.

Implements `IHostedService`, `IDisposable`.

Derived types: [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md).

## Constructors

### `TenantBackgroundService(ITenantScopeFactory<TKey>, ITenantLookup<TKey>, ILogger)`

Base class for a hosted service that does work for every active tenant once, then completes. Each tenant's work runs in its own scope, with a log scope with `TenantId` open on [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md). A tenant that fails is logged and the others go on; a tenant `ValidateTenantActivity` refuses is skipped.

```csharp
protected TenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantLookup<TKey> tenantLookup, ILogger logger)
```

Parameters:

- `scopeFactory` `ITenantScopeFactory<TKey>`: Creates a scope per tenant.
- `tenantLookup` `ITenantLookup<TKey>`: Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants; also [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md).

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your `DbContext`, repositories, …) from the scope's `ServiceProvider`, which is already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`. For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) instead.

## Properties

### `Logger`

The logger passed to the constructor, for the subclass's own messages.

```csharp
protected ILogger Logger { get; }
```

Value: `ILogger`

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

### `ExecuteForTenantAsync(ITenantScope<TKey>, CancellationToken)`

Performs the work for a single tenant. Called inside that tenant's scope, so scoped services resolved from its `ServiceProvider` see the tenant.

```csharp
protected abstract Task ExecuteForTenantAsync(ITenantScope<TKey> scope, CancellationToken cancellationToken)
```

Parameters:

- `scope` `ITenantScope<TKey>`: The tenant's scope: `Tenant` is the tenant being processed. The base class disposes it when this method returns.
- `cancellationToken` `CancellationToken`: A cancellation token tied to host shutdown.

Returns: `Task`

### `RunForAllTenantsAsync(CancellationToken)`

Runs [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) for every active tenant in the store, one at a time, with per-tenant failure isolation. Exposed so [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) can invoke it on each tick.

```csharp
protected Task RunForAllTenantsAsync(CancellationToken cancellationToken)
```

Parameters:

- `cancellationToken` `CancellationToken`: Stops the run: no tenant starts after it is cancelled.

Returns: `Task`
