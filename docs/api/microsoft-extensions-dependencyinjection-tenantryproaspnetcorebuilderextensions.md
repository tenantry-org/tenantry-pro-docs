# `TenantryProAspNetCoreBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Registers Tenantry.Pro's ASP.NET Core features on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProAspNetCoreBuilderExtensions
```

## Methods

### `AddTenantMetrics<TKey>(IProBuilder<TKey>, Action<TenantMetricsOptions<TKey>>?)`

Tags ASP.NET Core's request metrics (`http.server.request.duration`) with the request's tenant, as `tenant.id`, when `app.UseTenantry()` resolves it. An `OnResolved` handler of the application's own still runs, after the tag is added.

```csharp
public static IProBuilder<TKey> AddTenantMetrics<TKey>(this IProBuilder<TKey> builder, Action<TenantMetricsOptions<TKey>>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantMetricsOptions<TKey>>`: Sets [`TenantMetricsOptions<TKey>`](tenantry-pro-aspnetcore-tenantmetricsoptions.md), such as the tag's value for each tenant.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same builder for chaining.
