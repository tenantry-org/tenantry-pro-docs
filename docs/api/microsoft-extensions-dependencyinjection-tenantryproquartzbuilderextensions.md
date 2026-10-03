# `TenantryProQuartzBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.Quartz` · [API reference](README.md)

Extension methods for adding the Quartz.NET integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProQuartzBuilderExtensions
```

## Methods

### `AddQuartzPropagation<TKey>(IProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Makes a Quartz.NET job run as the tenant in its job data (set with `WithTenant(tenantId)` when the job is scheduled): the job, and the scoped services its constructor takes, are created as that tenant. Wire it into Quartz with `q.UseTenantry()`.

```csharp
public static IProBuilder<TKey> AddQuartzPropagation<TKey>(this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Sets what happens to a job that carries no tenant ([`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md), Warn by default) or a tenant the store does not have ([`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md), Reject by default).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

The application fails to start if Quartz's configuration never calls `q.UseTenantry()`, which would leave jobs without their tenant. Calling this again adds `configure` to the same options.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddQuartzPropagation()));
builder.Services.AddQuartz(q => q.UseTenantry());
builder.Services.AddQuartzHostedService();
```
