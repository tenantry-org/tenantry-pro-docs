# `ApplicationBuilderHangfireExtensions` class

Namespace: `Tenantry.Pro.Hangfire.Extensions` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's Hangfire integration into the ASP.NET Core application pipeline.

```csharp
public static class ApplicationBuilderHangfireExtensions
```

## Methods

### `UseTenantryHangfire(IApplicationBuilder)`

Adds [`TenantJobFilter<TKey>`](tenantry-pro-hangfire-filters-tenantjobfilter.md) to Hangfire's `GlobalJobFilters`, enabling tenant-context propagation for all enqueued jobs.

```csharp
public static IApplicationBuilder UseTenantryHangfire(this IApplicationBuilder app)
```

Parameters:

- `app` `IApplicationBuilder`: The application, whose services hold Hangfire's configuration.

Returns: `IApplicationBuilder`

This non-generic overload resolves `TKey` from DI automatically. Requires `pro.AddHangfireTenantFilter<TKey>()` to have been called during service registration.

```csharp
var app = builder.Build();
app.UseTenantryHangfire();  // TKey resolved from DI
app.UseHangfireDashboard();
app.Run();
```

### `UseTenantryHangfire<TKey>(IApplicationBuilder)`

Adds [`TenantJobFilter<TKey>`](tenantry-pro-hangfire-filters-tenantjobfilter.md) to Hangfire's `GlobalJobFilters`, enabling tenant-context propagation for all enqueued jobs.

```csharp
public static IApplicationBuilder UseTenantryHangfire<TKey>(this IApplicationBuilder app) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `app` `IApplicationBuilder`: The application builder.

Returns: `IApplicationBuilder`: The same `app` for chaining.

Call this after `app.Build()` and before `app.Run()`. Requires `pro.AddHangfireTenantFilter()` to have been called during service registration.

```csharp
var app = builder.Build();
app.UseTenantryHangfire<string>();
app.UseHangfireDashboard();
app.Run();
```
