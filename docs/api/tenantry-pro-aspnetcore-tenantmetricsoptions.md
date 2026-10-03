# `TenantMetricsOptions<TKey>` class

Namespace: `Tenantry.Pro.AspNetCore` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

How requests are tagged with their tenant. Set with `pro.AddTenantMetrics(o => ...)`.

```csharp
public sealed class TenantMetricsOptions<TKey> where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

## Properties

### `GetTagValue`

Returns the `tenant.id` tag for a tenant's requests, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) to leave the tag off them. When not set, the tag is the tenant id. Each distinct value is a separate series for every route, method and status code, so with many tenants, tag the ones you watch individually and group the rest. It is called for every request with a tenant, so it must be fast.

```csharp
public Func<ITenantDescriptor<TKey>, string?>? GetTagValue { get; set; }
```

Value: `Func<ITenantDescriptor<TKey>, string>`

```csharp
o.GetTagValue = tenant => tenant.TenantId.StartsWith("enterprise-") ? tenant.TenantId : "other";
```
