# `TenantryMassTransitSendContextExtensions` class

Namespace: `MassTransit` · Package: `Tenantry.Pro.MassTransit` · [API reference](README.md)

Extension methods for publishing or sending a message for a tenant other than the current one.

```csharp
public static class TenantryMassTransitSendContextExtensions
```

## Methods

### `WithTenant<TKey>(SendContext, ITenantDescriptor<TKey>)`

Makes the message carry `tenant`'s id, so it is consumed as that tenant. Prefer it to the id overload when you hold the tenant: the key type then comes from it, and a value of the wrong type, which would fail when the message is consumed, does not compile.

```csharp
public static SendContext WithTenant<TKey>(this SendContext context, ITenantDescriptor<TKey> tenant) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `context` `SendContext`: The message's context, in the callback that `Publish` or `Send` takes.
- `tenant` `ITenantDescriptor<TKey>`: The tenant.

Returns: `SendContext`: The same `context`.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

It sets the `HeaderName` header. MassTransit runs the callback after the bus's filters, Tenantry's among them, so the tenant it sets is the one the message carries.

```csharp
await bus.Publish(new InvoiceDue(invoiceId), context => context.WithTenant(tenantId));
```

### `WithTenant<TKey>(SendContext, TKey)`

Makes the message carry `tenantId`, so it is consumed as that tenant (`pro.AddMassTransitPropagation()`), whichever tenant is current, if any: for publishing on a tenant's behalf from code that runs without one, such as a scheduled task or an administrator's request.

```csharp
public static SendContext WithTenant<TKey>(this SendContext context, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `context` `SendContext`: The message's context, in the callback that `Publish` or `Send` takes.
- `tenantId` `TKey`: The tenant's id. The tenant is looked up when the message is consumed.

Returns: `SendContext`: The same `context`.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

It sets the `HeaderName` header. MassTransit runs the callback after the bus's filters, Tenantry's among them, so the tenant it sets is the one the message carries.

```csharp
await bus.Publish(new InvoiceDue(invoiceId), context => context.WithTenant(tenantId));
```
