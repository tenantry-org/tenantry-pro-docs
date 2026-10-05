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

Whether every database or schema offboarding drops for the tenant no longer exists.

```csharp
public bool DataDropped { get; init; }
```

Value: `bool`

It is [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) when an earlier offboarding that failed later dropped them, or they were never created. A step that reads the tenant's data should then do nothing: it ran in that earlier offboarding, before the drops. It is [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) while any of them exists, so with more than one, a step that runs again may find some already gone. It is [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) too when offboarding drops nothing for the tenant: no drop step is added, or none applies to the tenant's isolation.

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
