# `ITenantProvisioner<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Provisions a new tenant: runs every [`ITenantProvisioningStep<TKey>`](tenantry-pro-itenantprovisioningstep.md) for it, in order, and reports each step's outcome. `UsePro` registers it, as a singleton.

The steps run in this order: creating the tenant's database or schema (`AddDatabaseProvisioning`,     `AddSchemaProvisioning`), applying migrations (`AddMigrations`), then the steps and     seeders added with [`IProBuilder<TKey>.AddProvisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md) and     [`IProBuilder<TKey>.AddSeeder<TSeeder>`](tenantry-pro-iprobuilder-1.md), in the order they were added. With none registered,     provisioning succeeds and does nothing.

It does not add the tenant to your store: add it first, then pass its descriptor. To recover from a     failure, provision the tenant again, which runs every step again, so steps and seeders must be idempotent.

```csharp
public interface ITenantProvisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `ProvisionAsync(ITenantDescriptor<TKey>, CancellationToken)`

Runs the provisioning steps for `tenant`.

```csharp
Task<TenantProvisioningResult<TKey>> ProvisionAsync(ITenantDescriptor<TKey> tenant, CancellationToken cancellationToken = default)
```

Parameters:

- `tenant` `ITenantDescriptor<TKey>`: The tenant to provision, already in the tenant store.
- `cancellationToken` `CancellationToken`: Cancels provisioning.

Returns: `Task<TenantProvisioningResult<TKey>>`: Each step's outcome. A step that throws is reported in the result, not thrown; by default the steps after it do not run ([`TenantProvisioningOptions.StopOnFailure`](tenantry-pro-tenantprovisioningoptions.md)).

Exceptions:

- `ArgumentNullException`: `tenant` is null.
- `ArgumentException`: The tenant's id is one Tenantry reserves for "no tenant": the key type's default (`Guid.Empty`, `0`) or an empty string. No step runs.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid. No step runs.
- `InvalidOperationException`: In mixed mode, [`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md) returned a value that is not a [`TenantIsolation`](tenantry-pro-tenantisolation.md). No step runs. An exception `GetIsolation` throws is not caught either.
- `OperationCanceledException`: `cancellationToken` was cancelled. Provisioning stops: no further step starts, and steps that completed are not undone. A step that fails after the cancellation, in whatever way, is reported this way too, with its exception as the inner exception.
