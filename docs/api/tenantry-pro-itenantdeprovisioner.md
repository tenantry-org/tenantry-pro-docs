# `ITenantDeprovisioner<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Offboards a tenant, the reverse of [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md): runs the deprovisioning steps and reports each one.

Each step runs in its own tenant scope, in this order: the application's steps     ([`IProBuilder<TKey>.AddDeprovisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md)) in the order added; `DeleteSharedData`     (`AddSharedDataDeletion`); `DropDatabase` or `DropSchema` (`AddDatabaseDeprovisioning`,     `AddSchemaDeprovisioning`); then `ClearCaches`, which calls     `InvalidateAsync(TKey, CancellationToken)`. The first failed step stops the rest, which are     reported as not run, so a failed export never lets a drop run.

Suspend the tenant in your store first, so a `ValidateTenantActivity` validator refuses it, and remove it     from the store once the result succeeds: a migration run reports a tenant whose database is gone as failed.     Before any step, this instance's cached copy of the tenant is cleared and the store read again; a tenant the     store still has and that is active is refused. Other instances keep their cached copy until it expires     (`CacheTenants`), unless Tenantry Core broadcasts invalidations to them (`BroadcastInvalidations`,     in its tenant stores guide under "Several instances") and the tenant is invalidated when it is suspended;     otherwise wait that long after suspending before offboarding. Work that started before the suspension can     still be running.

Running it again after a failure is safe: a missing database or schema counts as dropped. The application's     steps run again too; when every drop finds its database or schema already gone they are told so     ([`TenantDeprovisioningContext<TKey>.DataDropped`](tenantry-pro-tenantdeprovisioningcontext.md)), and one that reads it should do nothing. `ClearCaches`     does not run after a failed step; call `InvalidateAsync(TKey, CancellationToken)` yourself if you need     the caches cleared.

`UsePro` registers it, as a singleton.

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
- `cancellationToken` `CancellationToken`: Cancels offboarding: the step in progress is cancelled, no further step starts, and steps that completed are not undone.

Returns: `Task<TenantDeprovisioningResult<TKey>>`: A result for each step: succeeded, failed, skipped (it does not apply to the tenant) or not run.

Exceptions:

- `ArgumentNullException`: `tenant` is null.
- `ArgumentException`: The tenant's id is one Tenantry reserves for "no tenant".
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; no step ran.
- `InvalidOperationException`: The tenant store has the tenant and it is active; with schema per tenant, more than one context uses `UseTenantry()` and none is listed, or a context it applies to does not use it; or, in mixed mode, [`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md) returned a value that is not a [`TenantIsolation`](tenantry-pro-tenantisolation.md), or [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) without `UseSchemaPerTenant`. No step ran.
- `OperationCanceledException`: `cancellationToken` was cancelled. A step that fails after the cancellation, in whatever way, is reported this way too, with its exception as the inner exception.

```csharp
var result = await deprovisioner.DeprovisionAsync(tenant, ct);
if (result.Succeeded)
    await tenants.RemoveAsync(tenant.TenantId, ct);
```
