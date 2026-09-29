# `MassTransitProBuilderExtensions` class

Namespace: `Tenantry.Pro.MassTransit.Extensions` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for registering MassTransit tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class MassTransitProBuilderExtensions
```

## Methods

### `AddMassTransitTenantFilters<TKey>(ProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Registers the MassTransit tenant-propagation filters as singletons in DI. After registration, call `cfg.UseTenantryPro(ctx)` (or the generic overload) inside your `AddMassTransit` bus factory configuration to wire the filters into the pipeline.

```csharp
public static ProBuilder<TKey> AddMassTransitTenantFilters<TKey>(this ProBuilder<TKey> builder, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Optional configuration of the tenant-propagation policy — in particular [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-backgroundservices-tenantpropagationoptions.md), which controls what happens to a consumed message that has no resolvable tenant. Defaults to `Warn`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.
