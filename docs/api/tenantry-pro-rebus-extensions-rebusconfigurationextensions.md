# `RebusConfigurationExtensions` class

Namespace: `Tenantry.Pro.Rebus.Extensions` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Extension methods for wiring Tenantry tenant steps into the Rebus pipeline.

```csharp
public static class RebusConfigurationExtensions
```

## Methods

### `UseTenantryPro(OptionsConfigurer, IServiceProvider)`

Decorates the Rebus pipeline with Tenantry tenant propagation steps. Call this inside `services.AddRebus(c => c.Options(o => o.UseTenantryPro(sp)))`.

```csharp
public static void UseTenantryPro(this OptionsConfigurer configurer, IServiceProvider sp)
```

Parameters:

- `configurer` `OptionsConfigurer`: The Rebus options configurer.
- `sp` `IServiceProvider`: The application `IServiceProvider` used to resolve the registered `IRebusConfigurator`.
