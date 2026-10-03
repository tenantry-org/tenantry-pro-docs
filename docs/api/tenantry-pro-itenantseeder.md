# `ITenantSeeder<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Writes a new tenant's initial data, as a step of provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)). Add one with [`IProBuilder<TKey>.AddSeeder<TSeeder>`](tenantry-pro-iprobuilder-1.md): seeders and the steps you add run after Tenantry.Pro's own steps, in the order you add them. A seeder registered only in the service collection is not run.

A seeder is resolved from a new scope for the tenant being provisioned, so it can take scoped services such     as a `DbContext` in its constructor, and they read and write as that tenant.

Make it idempotent: recovering from a failure means provisioning the tenant again, which runs every seeder     again, possibly after an earlier attempt wrote some of its rows.

```csharp
public interface ITenantSeeder<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `SeedAsync(ITenantDescriptor<TKey>, CancellationToken)`

Writes the initial data of `tenant`.

```csharp
Task SeedAsync(ITenantDescriptor<TKey> tenant, CancellationToken cancellationToken)
```

Parameters:

- `tenant` `ITenantDescriptor<TKey>`: The tenant being provisioned, which is also the current tenant.
- `cancellationToken` `CancellationToken`: Cancels provisioning.

Returns: `Task`
