# `ITenantProvisioningStep<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

A step [`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md) runs to provision a tenant. Add one with [`IProBuilder<TKey>.AddProvisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md).

Each step is resolved from a new scope for the tenant being provisioned, which is disposed when the step ends,     so it can take scoped services such as a `DbContext` in its constructor, and they read and write as that     tenant. It is resolved before [`ITenantProvisioningStep<TKey>.AppliesTo`](tenantry-pro-itenantprovisioningstep.md) is called, for every tenant: a service that cannot be     created for tenants the step does not apply to (a context for a database they do not have, say) belongs in     [`ITenantProvisioningStep<TKey>.ExecuteAsync`](tenantry-pro-itenantprovisioningstep.md), resolved from [`TenantProvisioningContext<TKey>.Scope`](tenantry-pro-tenantprovisioningcontext.md).

Make it idempotent: recovering from a failure means provisioning the tenant again, which runs every step     again, possibly after an earlier attempt did part of its work.

```csharp
public interface ITenantProvisioningStep<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `AppliesTo(TenantProvisioningContext<TKey>)`

Returns whether the step applies to the tenant being provisioned; a step that does not is reported as [`TenantProvisioningStepStatus.Skipped`](tenantry-pro-tenantprovisioningstepstatus.md). By default it applies to every tenant.

```csharp
bool AppliesTo(TenantProvisioningContext<TKey> context)
```

Parameters:

- `context` [`TenantProvisioningContext<TKey>`](tenantry-pro-tenantprovisioningcontext.md): The tenant being provisioned.

Returns: `bool`: [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) to run the step for this tenant.

In mixed mode, [`TenantProvisioningContext<TKey>.Isolation`](tenantry-pro-tenantprovisioningcontext.md) says where the tenant's data lives, so a step can apply only to tenants with their own database, say.

### `ExecuteAsync(TenantProvisioningContext<TKey>, CancellationToken)`

Runs the step for the tenant being provisioned.

```csharp
Task ExecuteAsync(TenantProvisioningContext<TKey> context, CancellationToken cancellationToken)
```

Parameters:

- `context` [`TenantProvisioningContext<TKey>`](tenantry-pro-tenantprovisioningcontext.md): The tenant being provisioned, and its scope.
- `cancellationToken` `CancellationToken`: Cancels provisioning.

Returns: `Task`
