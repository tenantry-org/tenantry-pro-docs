# `HangfireProBuilderExtensions` class

Namespace: `Tenantry.Pro.Hangfire.Extensions` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for registering Hangfire tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class HangfireProBuilderExtensions
```

## Methods

### `AddHangfireTenantFilter<TKey>(ProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Registers [`TenantJobFilter<TKey>`](tenantry-pro-hangfire-filters-tenantjobfilter.md) as a singleton in DI. After building the application, call `app.UseTenantryHangfire<TKey>()` to add the filter to Hangfire's `GlobalJobFilters`.

```csharp
public static ProBuilder<TKey> AddHangfireTenantFilter<TKey>(this ProBuilder<TKey> builder, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Optional configuration of the tenant-propagation policy — in particular [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-backgroundservices-tenantpropagationoptions.md), which controls what happens to a job that has no resolvable tenant. Defaults to `Warn`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.
