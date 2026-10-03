# `TenantDeprovisioningContext<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

What a deprovisioning step works with: the tenant being offboarded, a scope of its own, and its isolation.

```csharp
public sealed class TenantDeprovisioningContext<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `DataDropped`

Whether the tenant's database or schema, which offboarding drops, no longer exists: an earlier offboarding that failed later dropped it, or it was never created. A step that reads the tenant's data should then do nothing: it ran in that earlier offboarding, before the drop.

```csharp
public bool DataDropped { get; init; }
```

Value: `bool`

### `Isolation`

The tenant's isolation in mixed mode (`UseMixedMode`), or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) without it.

```csharp
public TenantIsolation? Isolation { get; init; }
```

Value: `TenantIsolation?`

### `Scope`

The step's tenant scope: the tenant is current, and its services come from here.

```csharp
public required ITenantScope<TKey> Scope { get; init; }
```

Value: `ITenantScope<TKey>`

### `Tenant`

The tenant being offboarded.

```csharp
public required ITenantDescriptor<TKey> Tenant { get; init; }
```

Value: `ITenantDescriptor<TKey>`
