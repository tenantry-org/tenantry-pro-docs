# `AuditTiming` enum

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

When audit logging passes a save's entries to [`IAuditStore`](tenantry-pro-efcore-iauditstore.md).

```csharp
public enum AuditTiming
```

## Values

| Value | Description |
|-------|-------------|
| `AfterCommit = 0` | Once the changes are committed: when `SaveChanges` completes, or, for changes saved in a transaction, when it commits, through whichever context (a transaction begun with `Database.BeginTransaction`, a `TransactionScope`, or one the connection was enlisted in). The entries of a transaction rolled back, or of changes rolled back to a savepoint, are discarded. A store failure is logged, and the save or commit still succeeds: its changes are in the database. A transaction begun outside an audited context and handed to one with `UseTransaction` is the exception: its entries are written when `SaveChanges` completes. |
| `InTransaction = 1` | At the end of each `SaveChanges`, inside the transaction the changes were saved in, so a store that writes through the saving context's connection and transaction commits or rolls back with them. A store failure, or cancellation, is thrown from `SaveChanges`, after the changes were sent and the change tracker accepted them: roll back and discard the context, or save with `acceptAllChangesOnSuccess: false` to retry. Without a transaction of your own, the changes are already committed when the store is called. |
