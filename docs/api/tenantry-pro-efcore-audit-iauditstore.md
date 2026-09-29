# `IAuditStore` interface

Namespace: `Tenantry.Pro.EfCore.Audit` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Receives audit entries produced by `Tenantry.Pro.EfCore.Audit` after each successful EF Core `SaveChanges` call.

The default implementation logs entries to `ILogger`. Replace it with your own implementation to persist entries to a database, event stream, or external audit service:

```csharp
builder.Services.AddSingleton<IAuditStore, MyDatabaseAuditStore>();
```

Register the replacement *after* calling `pro.AddAuditLogging()` to override the default.

```csharp
public interface IAuditStore
```

## Methods

### `SaveAsync(IReadOnlyList<AuditEntry>, CancellationToken)`

Persists or dispatches the supplied audit entries. Called once per `SaveChanges` call that produced at least one auditable change.

```csharp
Task SaveAsync(IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
```

Parameters:

- `entries` `IReadOnlyList<AuditEntry>`: The entries recorded in this save operation. Never empty.
- `cancellationToken` `CancellationToken`: A cancellation token.

Returns: `Task`
