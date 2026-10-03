# `TenantryProApplicationBuilderExtensions` class

Namespace: `Microsoft.AspNetCore.Builder` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Adds Tenantry.Pro's middleware to the request pipeline.

```csharp
public static class TenantryProApplicationBuilderExtensions
```

## Methods

### `UseTenantryMetrics(IApplicationBuilder)`

Adds the middleware that tags ASP.NET Core's request metrics with the tenant. Place it after `UseTenantry()`, which makes the tenant current: a request reaching it without a tenant is not tagged.

```csharp
public static IApplicationBuilder UseTenantryMetrics(this IApplicationBuilder app)
```

Parameters:

- `app` `IApplicationBuilder`: The application's request pipeline.

Returns: `IApplicationBuilder`: The same `app` for chaining.

Exceptions:

- `InvalidOperationException`: `pro.AddTenantMetrics()` was not called.

```csharp
app.UseTenantry();
app.UseTenantryMetrics(); // after UseTenantry()
```
