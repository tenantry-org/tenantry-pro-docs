# `MixedModeOptions<TKey>` class

Namespace: `Tenantry.Pro.Strategies.MixedMode` · Package: `Tenantry.Pro` · [API reference](README.md)

Options for the mixed-mode strategy, which routes individual tenants to either database-per-tenant or schema-per-tenant isolation.

```csharp
public sealed class MixedModeOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `GetStrategyForTenant`

A delegate that returns the isolation strategy for the given tenant descriptor.

```csharp
public Func<ITenantDescriptor<TKey>, TenantStrategy>? GetStrategyForTenant { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, TenantStrategy>`

```csharp
opts.GetStrategyForTenant = tenant => tenant.TenantId.StartsWith("enterprise_")
    ? TenantStrategy.Database
    : TenantStrategy.Schema;
```
