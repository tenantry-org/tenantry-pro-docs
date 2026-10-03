# `TenantryMassTransitBusFactoryConfiguratorExtensions` class

Namespace: `MassTransit` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's MassTransit integration into a bus.

```csharp
public static class TenantryMassTransitBusFactoryConfiguratorExtensions
```

## Methods

### `UseTenantry(IBusFactoryConfigurator, IBusRegistrationContext)`

Adds Tenantry's publish, send, consume and routing-slip activity filters to the bus, for every receive endpoint: a message published or sent while a tenant is current carries it (or the tenant set with `SetTenant`), and is consumed as that tenant. Requires `pro.AddMassTransitPropagation()`.

```csharp
public static void UseTenantry(this IBusFactoryConfigurator configurator, IBusRegistrationContext context)
```

Parameters:

- `configurator` `IBusFactoryConfigurator`: The bus configuration, in the transport's callback (`UsingRabbitMq` and the like).
- `context` `IBusRegistrationContext`: The bus registration context the callback is given.

Exceptions:

- `InvalidOperationException`: `pro.AddMassTransitPropagation()` was not called.

It covers every receive endpoint, configured before or after it, by `ConfigureEndpoints` or by hand: consumers and sagas (through MassTransit's scoped filters, created in each message's scope), handlers (`e.Handler<T>(…)`), and the execute and compensate steps of routing-slip activities (MassTransit Courier). Call it for each bus: with MassTransit's MultiBus, the application fails to start if a bus's configuration does not call it. Calling it again for the same bus has no further effect.
