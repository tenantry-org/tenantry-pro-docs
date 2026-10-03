# `ITenantDeprovisioningStep<TKey>` interface

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

A step of offboarding a tenant ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)): export its data, archive it, tell another system. Add one with [`IProBuilder<TKey>.AddDeprovisioningStep<TStep>`](tenantry-pro-iprobuilder-1.md); the application's steps run before Tenantry drops anything.

```csharp
public interface ITenantDeprovisioningStep<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Methods

### `AppliesTo(TenantDeprovisioningContext<TKey>)`

Whether the step applies to the tenant; one that does not is reported as skipped. By default, every tenant.

```csharp
bool AppliesTo(TenantDeprovisioningContext<TKey> context)
```

Parameters:

- `context` [`TenantDeprovisioningContext<TKey>`](tenantry-pro-tenantdeprovisioningcontext.md): The tenant being offboarded, its scope and its isolation.

Returns: `bool`: [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) to run the step.

### `ExecuteAsync(TenantDeprovisioningContext<TKey>, CancellationToken)`

Runs the step. An exception fails it, and stops the steps after it.

```csharp
Task ExecuteAsync(TenantDeprovisioningContext<TKey> context, CancellationToken cancellationToken)
```

Parameters:

- `context` [`TenantDeprovisioningContext<TKey>`](tenantry-pro-tenantdeprovisioningcontext.md): The tenant being offboarded, its scope and its isolation.
- `cancellationToken` `CancellationToken`: Cancels the step.

Returns: `Task`: A task that completes when the step is done.
