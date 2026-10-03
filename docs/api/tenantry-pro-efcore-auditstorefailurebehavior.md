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
| `Throw = 1` | `SaveChanges`, or the commit, throws an [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md) once every entry it could write is written. In a `TransactionScope` or an enlisted transaction, whose completion Tenantry cannot throw from, the failure is logged. |
