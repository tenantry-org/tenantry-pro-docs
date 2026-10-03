# `TenantryRebusOptionsConfigurerExtensions` class

Namespace: `Rebus.Config` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's Rebus integration into a bus.

```csharp
public static class TenantryRebusOptionsConfigurerExtensions
```

## Methods

### `UseTenantry(OptionsConfigurer, IServiceProvider)`

Adds Tenantry's steps to the bus's pipeline: a message sent or published while a tenant is current carries it, and is handled as that tenant. Requires `pro.AddRebusPropagation()`.

```csharp
public static void UseTenantry(this OptionsConfigurer configurer, IServiceProvider services)
```

Parameters:

- `configurer` `OptionsConfigurer`: Rebus's options.
- `services` `IServiceProvider`: The application's services, where `pro.AddRebusPropagation()` registered the integration: the `sp` that `services.AddRebus((configure, sp) => …)` passes.

Exceptions:

- `InvalidOperationException`: `pro.AddRebusPropagation()` was not called.

The incoming step runs just before Rebus deserializes the message, so after its retry step: a message     rejected by the policy, or whose tenant lookup throws, is retried and then moved to the error queue like     any other failed message. A pipeline without Rebus's `DeserializeIncomingMessageStep` fails to     start rather than handle messages outside their tenant. Steps Rebus runs before deserializing (loading a     data bus attachment, decrypting) run without the tenant.

Calling it again for the same bus has no further effect.
