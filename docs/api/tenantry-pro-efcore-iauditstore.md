# `IAuditStore` interface

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Receives the entries audit logging (`pro.AddAuditLogging()`) records for the changes a context saves.

The default implementation logs entries to `ILogger`. To persist     entries to a database, event stream, or external audit service instead, register your own implementation     after calling `pro.AddAuditLogging()`, with any lifetime:

```csharp
builder.Services.AddScoped<IAuditStore, MyDatabaseAuditStore>();
```

It is resolved for each call from the saving context's scope, or a scope created for the call, so it may be     scoped (the audit logging guide's "Where the store and provider come from"). The saves it makes are not     audited. Do not save through the context being audited.

```csharp
public interface IAuditStore
```

## Methods

### `SaveAsync(DbContext, IReadOnlyList<AuditEntry>, CancellationToken)`

Persists or dispatches the supplied audit entries: once their changes are committed, by default, or inside the transaction they were saved in ([`AuditOptions.Timing`](tenantry-pro-efcore-auditoptions.md)).

```csharp
Task SaveAsync(DbContext context, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
```

Parameters:

- `context` `DbContext`: The context that saved the changes. With [`AuditTiming.InTransaction`](tenantry-pro-efcore-audittiming.md), its `Database.GetDbConnection()` and `Database.CurrentTransaction` are the connection and transaction they were saved in. With [`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md), do not save through it. When a transaction commits after the context was disposed, or after a pooled context went back to its pool, read nothing from it but its type.
- `entries` `IReadOnlyList<AuditEntry>`: The entries for one save, or for one context's saves in one transaction. Never empty.
- `cancellationToken` `CancellationToken`: A cancellation token: the save's, with [`AuditTiming.InTransaction`](tenantry-pro-efcore-audittiming.md). With [`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md) it is never cancelled, so the entries of committed changes are not lost to a cancelled request.

Returns: `Task`
