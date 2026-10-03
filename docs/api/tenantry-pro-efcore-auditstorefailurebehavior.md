# `AuditStoreFailureBehavior` enum

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

What happens when [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) fails to write the entries of changes that are already saved, with [`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md). Set with [`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md).

```csharp
public enum AuditStoreFailureBehavior
```

## Values

| Value | Description |
|-------|-------------|
| `Log = 0` | The failure is logged as an error, and the save or commit succeeds. The default. |
| `Throw = 1` | `SaveChanges`, or the commit, throws an [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md), which holds the entries, once every entry it could write is written. The changes are saved all the same: do not save them again. In a `TransactionScope`, or a transaction the connection was enlisted in (`Database.EnlistTransaction`), whose completion Tenantry cannot throw from, the failure is logged. |
