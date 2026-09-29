# `BusRegistrationConfiguratorExtensions` class

Namespace: `Tenantry.Pro.MassTransit.Extensions` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for adding Tenantry.Pro's consume-side tenant filter to all MassTransit receive endpoints.

```csharp
public static class BusRegistrationConfiguratorExtensions
```

## Methods

### `AddTenantryConsumeFilter(IBusRegistrationConfigurator)`

Registers a callback that adds the tenant-context consume filter to every receive endpoint configured by `ConfigureEndpoints`. This non-generic overload resolves `TKey` from DI via `IMassTransitBusConfigurator`.

```csharp
public static IBusRegistrationConfigurator AddTenantryConsumeFilter(this IBusRegistrationConfigurator configurator)
```

Parameters:

- `configurator` `IBusRegistrationConfigurator`: The bus registration configurator.

Returns: `IBusRegistrationConfigurator`: The same `configurator` for chaining.

Call this inside `AddMassTransit`, before the transport configuration. Requires `pro.AddMassTransitTenantFilters()` to have been called during service registration.

### `AddTenantryConsumeFilter<TKey>(IBusRegistrationConfigurator)`

Registers a callback that adds the tenant-context consume filter to every receive endpoint configured by `ConfigureEndpoints`.

```csharp
public static IBusRegistrationConfigurator AddTenantryConsumeFilter<TKey>(this IBusRegistrationConfigurator configurator) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `configurator` `IBusRegistrationConfigurator`: The bus registration configurator.

Returns: `IBusRegistrationConfigurator`: The same `configurator` for chaining.

Call this inside `AddMassTransit`, before the transport configuration. Requires `pro.AddMassTransitTenantFilters()` to have been called during service registration.
