# `MixedModeOptions<TKey>` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Mixed mode: tenants with different isolation in one application. Set with `pro.UseMixedMode(o => o.GetIsolation = ...)`.

```csharp
public sealed class MixedModeOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `GetIsolation`

Returns where a tenant's data lives. It is called often, so it must be fast, and it must give the same answer for a tenant each time: compute it from something fixed when the tenant is created (its id, or a flag your store sets then), not from its plan. Tenantry does not move a tenant's data.

```csharp
public Func<ITenantDescriptor<TKey>, TenantIsolation>? GetIsolation { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, TenantIsolation>`

```csharp
o.GetIsolation = tenant => tenant.TenantId.StartsWith("enterprise-")
    ? TenantIsolation.Database
    : TenantIsolation.Shared;
```
