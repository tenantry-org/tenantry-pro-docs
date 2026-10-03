# `IAuditContextProvider` interface

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Says who made the changes a save records, and what ties them to the request or job that made them: the [`AuditEntry.Actor`](tenantry-pro-efcore-auditentry.md), [`AuditEntry.CorrelationId`](tenantry-pro-efcore-auditentry.md) and [`AuditEntry.Data`](tenantry-pro-efcore-auditentry.md) of each entry.

The default gives no actor, and the current trace's id (`Current`) as     the correlation id. To name the user, register your own, with any lifetime, for example from     `IHttpContextAccessor` in an ASP.NET Core application:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditContextProvider, HttpAuditContextProvider>();
```

It is called once for each save that has changes to record, before the changes are sent, in the code that     saves, so a failure stops the save. It is resolved as [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) is: for a context without a     scope of its own, read ambient state such as `IHttpContextAccessor` rather than scoped services.

```csharp
public interface IAuditContextProvider
```

## Methods

### `GetContext(DbContext)`

Gets who is making the changes `context` is about to save.

```csharp
AuditContext GetContext(DbContext context)
```

Parameters:

- `context` `DbContext`: The context that is saving.

Returns: [`AuditContext`](tenantry-pro-efcore-auditcontext.md): The actor, correlation id and data for every entry of the save.
