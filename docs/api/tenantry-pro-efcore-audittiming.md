# `AuditTiming` enum

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

When audit logging passes a save's entries to [`IAuditStore`](tenantry-pro-efcore-iauditstore.md).

```csharp
public enum AuditTiming
```

## Values

| Value | Description |
|-------|-------------|
| `AfterCommit = 0` | Once the changes are committed: when `SaveChanges` completes, or when the transaction they were saved in commits, through whichever context. Entries of rolled-back changes are discarded. |
| `InTransaction = 1` | At the end of each `SaveChanges`, inside the transaction the changes were saved in, so a store that writes through the saving context's connection and transaction commits or rolls back with them. |
