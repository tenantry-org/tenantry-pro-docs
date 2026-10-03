# `TenantryProHangfireBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for adding the Hangfire integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProHangfireBuilderExtensions
```

## Methods

### `AddHangfirePropagation<TKey>(IProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Makes a Hangfire job carry the tenant that was current when it was enqueued, and run as that tenant: its scoped services, created in the job's scope, see the tenant. Wire it into Hangfire with `UseTenantry(sp)` on Hangfire's configuration.

```csharp
public static IProBuilder<TKey> AddHangfirePropagation<TKey>(this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Sets what happens to a job that carries no tenant ([`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md), Warn by default) or a tenant the store does not have ([`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md), Reject by default).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

The application fails to start if Hangfire's configuration never calls `UseTenantry(sp)`, which would leave jobs without their tenant. Calling this again adds `configure` to the same options.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddHangfirePropagation()));
builder.Services.AddHangfire((sp, config) => config
    .UseSqlServerStorage(connectionString)
    .UseTenantry(sp));
```
