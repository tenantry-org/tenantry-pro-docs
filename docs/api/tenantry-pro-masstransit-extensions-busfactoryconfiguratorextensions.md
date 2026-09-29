# `BusFactoryConfiguratorExtensions` class

Namespace: `Tenantry.Pro.MassTransit.Extensions` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's publish and send filters into the MassTransit bus pipeline.

```csharp
public static class BusFactoryConfiguratorExtensions
```

## Methods

### `UseTenantryPro(IBusFactoryConfigurator, IBusRegistrationContext)`

Adds tenant-context publish and send pipeline filters to the MassTransit bus. Call this inside your `UsingRabbitMq` (or equivalent) callback.

```csharp
public static void UseTenantryPro(this IBusFactoryConfigurator configurator, IBusRegistrationContext context)
```

Parameters:

- `configurator` `IBusFactoryConfigurator`: The bus configuration.
- `context` `IBusRegistrationContext`: The bus registration context, which resolves the filters' dependencies.

This non-generic overload resolves `TKey` from DI automatically via `IMassTransitBusConfigurator`. Requires `pro.AddMassTransitTenantFilters<TKey>()` to have been called during service registration.

To also activate the consume-side scope, call     `x.AddTenantryConsumeFilter<TKey>()` inside your `AddMassTransit` callback.

```csharp
services.AddMassTransit(x =>
{
    x.AddTenantryConsumeFilter<string>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.UseTenantryPro(ctx);  // TKey resolved from DI
        cfg.ConfigureEndpoints(ctx);
    });
});
```

### `UseTenantryPro<TKey>(IBusFactoryConfigurator, IBusRegistrationContext)`

Adds tenant-context publish and send pipeline filters to the MassTransit bus. Call this inside your `UsingRabbitMq` (or equivalent) callback.

```csharp
public static void UseTenantryPro<TKey>(this IBusFactoryConfigurator configurator, IBusRegistrationContext context) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `configurator` `IBusFactoryConfigurator`: The bus factory configurator.
- `context` `IBusRegistrationContext`: The bus registration context, used to resolve filters from DI.

This method wires the publish and send filters only. To also activate the consume-side scope, call `x.AddTenantryConsumeFilter<TKey>()` inside your `AddMassTransit` callback.

Requires `pro.AddMassTransitTenantFilters()` to have been called during     service registration.

```csharp
services.AddMassTransit(x =>
{
    x.AddTenantryConsumeFilter<string>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.UseTenantryPro<string>(ctx);
        cfg.ConfigureEndpoints(ctx);
    });
});
```
