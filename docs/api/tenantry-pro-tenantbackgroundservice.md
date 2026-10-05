# `TenantBackgroundService<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Base class for a hosted service that does work for every active tenant once, then completes.

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your     `DbContext`, repositories) from the scope's `ServiceProvider`, which is     already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`.     For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) instead.

Each tenant runs in its own scope, with a log scope with `TenantId` open on [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md), and     [`TenantBackgroundService<TKey>.MaxConcurrency`](tenantry-pro-tenantbackgroundservice.md) says how many run at once (one by default). A tenant that fails is logged and     the others go on; a tenant `ValidateTenantActivity` refuses is skipped. If the tenant store cannot be     read, or [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) throws, the service throws, which by default stops the host.

Every instance of the application runs it, once each. Override [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) to run it on     one instance only.

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

Base class for a hosted service that does work for every active tenant once, then completes.

```csharp
protected TenantBackgroundService(ITenantScopeFactory<TKey> scopeFactory, ITenantLookup<TKey> tenantLookup, ILogger logger)
```

Parameters:

- `scopeFactory` `ITenantScopeFactory<TKey>`: Creates a scope per tenant.
- `tenantLookup` `ITenantLookup<TKey>`: Lists the tenants to run for.
- `logger` `ILogger`: Logs each tenant's failures, which do not stop the other tenants; also [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md).

Override [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) with the per-tenant work; resolve scoped services (your     `DbContext`, repositories) from the scope's `ServiceProvider`, which is     already bound to the tenant. Register the subclass with `builder.Services.AddHostedService<MySweep>()`.     For recurring work, derive from [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) instead.

Each tenant runs in its own scope, with a log scope with `TenantId` open on [`TenantBackgroundService<TKey>.Logger`](tenantry-pro-tenantbackgroundservice.md), and     [`TenantBackgroundService<TKey>.MaxConcurrency`](tenantry-pro-tenantbackgroundservice.md) says how many run at once (one by default). A tenant that fails is logged and     the others go on; a tenant `ValidateTenantActivity` refuses is skipped. If the tenant store cannot be     read, or [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) throws, the service throws, which by default stops the host.

Every instance of the application runs it, once each. Override [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) to run it on     one instance only.

## Properties

### `Logger`

The logger passed to the constructor, for the subclass's own messages.

```csharp
protected ILogger Logger { get; }
```

Value: `ILogger`

### `MaxConcurrency`

How many tenants a sweep works on at once. Default: 1, one after another, in the tenant store's order.

```csharp
protected virtual int MaxConcurrency { get; }
```

Value: `int`

Above 1, [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) runs for that many tenants in parallel, each still in its own     scope, so tenants finish in any order. The work must then be safe to run concurrently: two tenants'     scopes share every singleton.

A value below 1 makes the sweep throw `InvalidOperationException`; a     [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) throws it before its first sweep, which by default     stops the host.

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

Runs one sweep: when [`TenantBackgroundService<TKey>.ShouldRunAsync`](tenantry-pro-tenantbackgroundservice.md) returns [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), runs [`TenantBackgroundService<TKey>.ExecuteForTenantAsync`](tenantry-pro-tenantbackgroundservice.md) for every active tenant in the store.

```csharp
protected Task RunForAllTenantsAsync(CancellationToken cancellationToken)
```

Parameters:

- `cancellationToken` `CancellationToken`: Stops the sweep: no tenant starts after it is cancelled, the tenants running see it, and the sweep then throws `OperationCanceledException`.

Returns: `Task`

Exceptions:

- `InvalidOperationException`: [`TenantBackgroundService<TKey>.MaxConcurrency`](tenantry-pro-tenantbackgroundservice.md) is below 1.

[`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) calls it on each tick. A tenant that fails is logged with its id and the others go on, however many run at once.

### `ShouldRunAsync(CancellationToken)`

Says whether this instance of the application runs the next sweep. Returns [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) unless overridden.

```csharp
protected virtual ValueTask<bool> ShouldRunAsync(CancellationToken cancellationToken)
```

Parameters:

- `cancellationToken` `CancellationToken`: The sweep's cancellation token, tied to host shutdown.

Returns: `ValueTask<bool>`: [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) to run the sweep; [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) to skip it.

It is called before each sweep reads the tenant store. Override it to check that this instance is the     leader, or to take a lease, so that one instance of several runs the sweep. The base class has no call     after the sweep to release one, and the instances' sweeps start at different times, so for a periodic     service take a lease that lasts nearly the whole     [`PeriodicTenantBackgroundService<TKey>.Interval`](tenantry-pro-periodictenantbackgroundservice.md), and longer than a sweep: the other     instances then find it taken whenever they ask. A stored time of the last sweep, which the override     checks and moves on in one update, works the same way.

When it returns [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), the sweep ends without reading the store, and a debug message     says so. A [`PeriodicTenantBackgroundService<TKey>`](tenantry-pro-periodictenantbackgroundservice.md) calls it again at the next sweep. When it     throws, the sweep fails as it does when the store cannot be read.
