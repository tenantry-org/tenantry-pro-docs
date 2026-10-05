# `TenantryProMassTransitBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for adding the MassTransit integration on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProMassTransitBuilderExtensions
```

## Methods

### `AddMassTransitPropagation<TKey>(IProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Makes a message published or sent while a tenant is current carry the tenant in a header, and makes that tenant current while the message is consumed: the consumer, and the scoped services it takes, see it. Wire it into the bus with `cfg.UseTenantry(context)`.

```csharp
public static IProBuilder<TKey> AddMassTransitPropagation<TKey>(this IProBuilder<TKey> pro, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Sets what happens to a message that carries no tenant ([`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-tenantpropagationoptions.md), Warn by default) or a tenant the store does not have or `ValidateTenantActivity` refuses ([`TenantPropagationOptions.OnUnresolvedTenant`](tenantry-pro-tenantpropagationoptions.md), Reject by default).

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

The application fails to start if the bus configuration never calls `cfg.UseTenantry(context)`, which would leave messages without their tenant. Calling this again adds `configure` to the same options.

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddMassTransitPropagation()));
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseTenantry(context);
        cfg.ConfigureEndpoints(context);
    });
});
```
