# `TenantProvisioningContext<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

The tenant an [`ITenantProvisioningStep<TKey>`](tenantry-pro-itenantprovisioningstep.md) runs for.

```csharp
public sealed class TenantProvisioningContext<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `Isolation`

Where the tenant's data lives, from [`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) without mixed mode.

```csharp
public TenantIsolation? Isolation { get; init; }
```

Value: `TenantIsolation?`

### `Scope`

The step's scope, whose tenant is [`TenantProvisioningContext<TKey>.Tenant`](tenantry-pro-tenantprovisioningcontext.md). The step was resolved from it, and it is disposed when the step ends.

```csharp
public required ITenantScope<TKey> Scope { get; init; }
```

Value: `ITenantScope<TKey>`

### `Tenant`

The tenant being provisioned.

```csharp
public required ITenantDescriptor<TKey> Tenant { get; init; }
```

Value: `ITenantDescriptor<TKey>`
