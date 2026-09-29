# `ITenantLifecycleManager<TKey>` interface

Namespace: `Tenantry.Pro.Lifecycle` · Package: `Tenantry.Pro` · [API reference](README.md)

Orchestrates the full tenant creation lifecycle: provision → migrate → seed.

Each step is only executed when the corresponding service is registered in DI:

- Infrastructure provisioning: requires `AddDatabaseProvisioning()` or             `AddSchemaProvisioning()`.
- Migrations: requires `WithMigrationOrchestration()`.
- Seeding: requires a registered [`ITenantSeeder<TKey>`](tenantry-pro-lifecycle-itenantseeder.md).

    Any unregistered step is silently skipped.

The manager does **not** persist the tenant to the store — that is the caller's     responsibility before invoking [`ITenantLifecycleManager<TKey>.ProvisionAsync`](tenantry-pro-lifecycle-itenantlifecyclemanager.md).

```csharp
public interface ITenantLifecycleManager<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `ProvisionAsync(ITenantDescriptor<TKey>, CancellationToken)`

Runs the full provisioning pipeline for the given tenant and returns the outcome.

```csharp
Task<TenantProvisioningResult<TKey>> ProvisionAsync(ITenantDescriptor<TKey> tenant, CancellationToken cancellationToken = default)
```

Parameters:

- `tenant` [`ITenantDescriptor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantdescriptor): The tenant descriptor. The consumer must have already persisted this to their store before calling this method.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task<TenantProvisioningResult<TKey>>`: A [`TenantProvisioningResult<TKey>`](tenantry-pro-lifecycle-tenantprovisioningresult.md) describing which steps succeeded or failed. A step that fails is captured in the result rather than thrown.

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): Thrown before the pipeline starts if the licence key is missing or invalid.
- `OperationCanceledException`: `cancellationToken` was cancelled. The pipeline stops at once and steps that already completed are not undone.
