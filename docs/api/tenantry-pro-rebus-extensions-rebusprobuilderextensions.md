# `RebusProBuilderExtensions` class

Namespace: `Tenantry.Pro.Rebus.Extensions` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Extension methods for registering Rebus tenant-propagation services on [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md).

```csharp
public static class RebusProBuilderExtensions
```

## Methods

### `AddRebusTenantSteps<TKey>(ProBuilder<TKey>, Action<TenantPropagationOptions>?)`

Registers [`TenantOutgoingStep<TKey>`](tenantry-pro-rebus-steps-tenantoutgoingstep.md) and [`TenantIncomingStep<TKey>`](tenantry-pro-rebus-steps-tenantincomingstep.md) as singletons in DI. After registration, wire them into Rebus using `options.UseTenantryPro(sp)` inside your `services.AddRebus(...)` callback.

```csharp
public static ProBuilder<TKey> AddRebusTenantSteps<TKey>(this ProBuilder<TKey> builder, Action<TenantPropagationOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The Pro builder.
- `configure` `Action<TenantPropagationOptions>`: Optional configuration of the tenant-propagation policy — in particular [`TenantPropagationOptions.OnMissingTenant`](tenantry-pro-backgroundservices-tenantpropagationoptions.md), which controls what happens to an incoming message that has no resolvable tenant. Defaults to `Warn`.

Returns: [`ProBuilder<TKey>`](tenantry-pro-internal-probuilder.md): The same `builder` for chaining.
