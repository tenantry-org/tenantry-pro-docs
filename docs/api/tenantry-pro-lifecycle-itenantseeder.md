# `ITenantSeeder<TKey>` interface

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Consumer-implemented interface for seeding initial data into a new tenant's database. Register an implementation in DI before calling [`ITenantLifecycleManager<TKey>.ProvisionAsync`](tenantry-pro-lifecycle-itenantlifecyclemanager.md):

```csharp
services.AddScoped<ITenantSeeder<Guid>, MyTenantSeeder>();
```

```csharp
public interface ITenantSeeder<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `SeedAsync(ITenantDescriptor<TKey>, IServiceProvider, CancellationToken)`

Seeds default data for the given tenant. Called within a DI scope where all scoped services (`DbContext`, repositories, etc.) are tenant-aware — the tenant context is already set.

```csharp
Task SeedAsync(ITenantDescriptor<TKey> tenant, IServiceProvider scopedProvider, CancellationToken cancellationToken)
```

Parameters:

- `tenant` [`ITenantDescriptor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantdescriptor): The tenant being provisioned.
- `scopedProvider` `IServiceProvider`: An `IServiceProvider` scoped to this tenant. Resolve your `DbContext` or other services from here.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`
