# `ApplicationBuilderTelemetryExtensions` class

Namespace: `Tenantry.Pro.AspNetCore.Telemetry.Extensions` · Package: `Tenantry.Pro.AspNetCore` · [API reference](README.md)

Extension methods for wiring tenant metrics middleware into the ASP.NET Core pipeline.

```csharp
public static class ApplicationBuilderTelemetryExtensions
```

## Methods

### `UseTenantryMetrics(IApplicationBuilder)`

Adds the per-tenant request metrics middleware to the pipeline. Must be placed **after** `UseTenantry()` so that `ITenantContext<TKey>` is populated before metrics are recorded.

```csharp
public static IApplicationBuilder UseTenantryMetrics(this IApplicationBuilder app)
```

Parameters:

- `app` `IApplicationBuilder`: The application's request pipeline.

Returns: `IApplicationBuilder`

```csharp
app.UseTenantry();
app.UseTenantryMetrics(); // after UseTenantry()
```
