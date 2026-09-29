# `TelemetryProBuilderExtensions` class

Namespace: `Tenantry.Pro.AspNetCore.Telemetry.Extensions` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Extension methods for registering tenant metrics on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class TelemetryProBuilderExtensions
```

## Methods

### `AddTenantMetrics<TKey>(ProBuilder<TKey>, Action<TenantMetricsOptions>?)`

Registers per-tenant HTTP request metrics middleware. Call `app.UseTenantryMetrics()` after `app.UseTenantry()` to activate the middleware.

```csharp
public static ProBuilder<TKey> AddTenantMetrics<TKey>(this ProBuilder<TKey> builder, Action<TenantMetricsOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantMetricsOptions>`: Optional configuration for metrics behaviour.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same builder for chaining.
