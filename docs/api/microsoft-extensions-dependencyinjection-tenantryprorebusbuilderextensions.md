# `TenantryProRebusBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Extension methods for adding the Rebus integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProRebusBuilderExtensions
```

## Methods

### `AddRebusPropagation<TKey>(IProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Makes a message sent or published while a tenant is current carry the tenant in a header, and makes that tenant current while the message is handled: the handlers, and the scoped services they take, see it. Wire it into Rebus with `o.UseTenantry(sp)` in Rebus's options.

```csharp
public static IProBuilder<TKey> AddRebusPropagation<TKey>(this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Sets what happens to a message that carries no tenant ([`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md), Warn by default) or a tenant the store does not have or `ValidateTenantActivity` refuses ([`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md), Reject by default).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

The application fails to start if Rebus's configuration never calls `o.UseTenantry(sp)`, which would leave messages without their tenant. Calling this again adds `configure` to the same options.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddRebusPropagation()));
builder.Services.AddRebus((configure, sp) => configure
    .Transport(t => t.UseRabbitMq(connectionString, "orders"))
    .Options(o => o.UseTenantry(sp)));
```
