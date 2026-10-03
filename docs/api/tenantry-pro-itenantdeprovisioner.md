# `ITenantDeprovisioner<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Offboards a tenant, the reverse of [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md): runs the deprovisioning steps and reports each one. Registered by `UsePro`, as a singleton.

The steps run in this order, each in a tenant scope of its own: the application's steps     ([`IProBuilder<TKey>.AddDeprovisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md): export, archive, notify), in the order added;     then Tenantry.Pro.EfCore's: deleting its rows from the shared database, with `AddSharedDataDeletion`, then     dropping its database or schema, with `AddDatabaseDeprovisioning` or `AddSchemaDeprovisioning`; then `ClearCaches`, which     invalidates the tenant (`InvalidateAsync(TKey, CancellationToken)`), and with it every cache Tenantry     keeps for it. The first step that fails stops the rest, which are reported as not run: a failed export never     lets the drop run.

Suspend the tenant in your store first, so a `ValidateTenantActivity` validator refuses it, and remove it     from the store once the result succeeds: a migration run reports a tenant whose database is gone as failed.     Before any step, this instance's cached copy of the tenant is cleared and the store read again; a tenant the     store still has and that is active is refused. Other instances keep their cached copy until it expires     (`CacheTenants`), so with several instances wait that long after suspending, or clear it on each, before     offboarding. Work that started before the suspension can still be running.

Running it again after a failure is safe: a missing database or schema counts as dropped. The application's     steps run again too; when a drop finds the data already gone they are told so     ([`TenantDeprovisioningContext<TKey>.DataDropped`](tenantry-pro-tenantdeprovisioningcontext.md)), and one that reads it should do nothing. `ClearCaches`     does not run after a failed step; call `InvalidateAsync(TKey, CancellationToken)` yourself if you need     the caches cleared.

```csharp
public interface ITenantDeprovisioner<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `DeprovisionAsync(ITenantDescriptor<TKey>, CancellationToken)`

Runs the deprovisioning steps for `tenant`.

```csharp
Task<TenantDeprovisioningResult<TKey>> DeprovisionAsync(ITenantDescriptor<TKey> tenant, CancellationToken cancellationToken = default)
```

Parameters:

- `tenant` `ITenantDescriptor<TKey>`: The tenant to offboard.
- `cancellationToken` `CancellationToken`: Stops before the next step.

Returns: `Task<TenantDeprovisioningResult<TKey>>`: A result for each step: succeeded, failed, skipped (it does not apply to the tenant) or not run.

Exceptions:

- `ArgumentNullException`: `tenant` is null.
- `ArgumentException`: The tenant's id is one Tenantry reserves for "no tenant".
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; no step ran.
- `InvalidOperationException`: The tenant store has the tenant and it is active, or with schema per tenant more than one context uses `UseTenantry()` and none is listed; no step ran.
- `OperationCanceledException`: `cancellationToken` was cancelled.

```csharp
var result = await deprovisioner.DeprovisionAsync(tenant, ct);
if (result.Succeeded)
    await tenants.RemoveAsync(tenant.TenantId, ct);
```
