# `TenantryRebusHeadersExtensions` class

Namespace: `Rebus.Bus` · Package: `Tenantry.Pro.Rebus` · [API reference](README.md)

Extension methods for sending or publishing a message for a tenant other than the current one.

```csharp
public static class TenantryRebusHeadersExtensions
```

## Methods

### `WithTenant<TKey>(IDictionary<string, string>, TKey)`

Adds `tenantId` to the headers a message is sent or published with, so it is handled as that tenant (`pro.AddRebusPropagation()`), whichever tenant is current, if any: for sending on a tenant's behalf from code that runs without one, such as a scheduled task or an administrator's request.

```csharp
public static IDictionary<string, string> WithTenant<TKey>(this IDictionary<string, string> headers, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `headers` `IDictionary<string, string>`: The headers to send the message with, as passed to `Send`, `Publish` or `Defer`.
- `tenantId` `TKey`: The tenant's id. The tenant is looked up when the message is handled.

Returns: `IDictionary<string, string>`: The same `headers` for chaining.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

It sets the [`TenantPropagation.HeaderName`](tenantry-pro-tenantpropagation.md) header, which Tenantry's outgoing step then leaves in place.

```csharp
await bus.Send(new InvoiceDue(invoiceId), new Dictionary<string, string>().WithTenant(tenantId));
```
