# `TenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro.BackgroundServices` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that performs work for every tenant once, then completes. Each tenant is processed inside its own combined DI + tenant scope, and a failure processing one tenant is logged and isolated so it never aborts the sweep of the remaining tenants.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your `DbContext`, repositories, …) from the supplied `IServiceProvider`, which is already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`. For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) instead.

```csharp
public abstract class TenantBackgroundService<TKey> : BackgroundService, IHostedService, IDisposable where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Inherits `BackgroundService`.

Implements `IHostedService`, `IDisposable`.

Derived types: [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md).

## Constructors

### `TenantBackgroundService(ITenantScopeFactory<TKey>, ITenantStoreAccessor<TKey>, ILogger)`

Base class for a hosted service that performs work for every tenant once, then completes. Each tenant is processed inside its own combined DI + tenant scope, and a failure processing one tenant is logged and isolated so it never aborts the sweep of the remaining tenants.

```csharp
protected TenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantStoreAccessor<TKey> storeAccessor, ILogger logger)
```

Parameters:

- `scopeFactory` [`ITenantScopeFactory<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantscopefactory): Creates a scope per tenant.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your `DbContext`, repositories, …) from the supplied `IServiceProvider`, which is already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`. For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) instead.

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

### `ExecuteForTenantAsync(ITenantDescriptor<TKey>, IServiceProvider, CancellationToken)`

Performs the work for a single tenant. Called inside that tenant's scope, so scoped services resolved from `scopedServices` see the tenant.

```csharp
protected abstract Task ExecuteForTenantAsync(ITenantDescriptor<TKey> tenant, IServiceProvider scopedServices, CancellationToken cancellationToken)
```

Parameters:

- `tenant` [`ITenantDescriptor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantdescriptor): The tenant being processed.
- `scopedServices` `IServiceProvider`: A service provider scoped to `tenant`.
- `cancellationToken` `CancellationToken`: A cancellation token tied to host shutdown.

Returns: `Task`

### `RunForAllTenantsAsync(CancellationToken)`

Runs [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-backgroundservices-tenantbackgroundservice.md) for every tenant in the store, one at a time, with per-tenant failure isolation. Exposed so [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-backgroundservices-periodictenantbackgroundservice.md) can invoke it on each tick.

```csharp
protected Task RunForAllTenantsAsync(CancellationToken cancellationToken)
```

Parameters:

- `cancellationToken` `CancellationToken`: Stops the run: no tenant starts after it is cancelled.

Returns: `Task`
