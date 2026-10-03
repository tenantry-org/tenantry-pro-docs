# `TenantryMassTransitSendContextExtensions` class

Namespace: `MassTransit` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for publishing or sending a message for a tenant other than the current one.

```csharp
public static class TenantryMassTransitSendContextExtensions
```

## Methods

### `SetTenant<TKey>(SendContext, TKey)`

Makes the message carry `tenantId`, so it is consumed as that tenant (`pro.AddMassTransitPropagation()`), whichever tenant is current, if any: for publishing on a tenant's behalf from code that runs without one, such as a scheduled task or an administrator's request.

```csharp
public static void SetTenant<TKey>(this SendContext context, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `context` `SendContext`: The message's context, in the callback that `Publish` or `Send` takes.
- `tenantId` `TKey`: The tenant's id. The tenant is looked up when the message is consumed.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

It sets the [`TenantPropagation.HeaderName`](tenantry-pro-tenantpropagation.md) header. MassTransit runs the callback after the bus's filters, Tenantry's among them, so the tenant it sets is the one the message carries.

```csharp
await bus.Publish(new InvoiceDue(invoiceId), context => context.SetTenant(tenantId));
```
