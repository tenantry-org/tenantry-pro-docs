# `AuditStoreException` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Thrown from `SaveChanges`, or from a transaction's commit, when [`IAuditStore`](tenantry-pro-efcore-iauditstore.md) failed to write the entries of changes that were saved, with [`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md) set to [`AuditStoreFailureBehavior.Throw`](tenantry-pro-efcore-auditstorefailurebehavior.md).

The changes are saved, and committed unless they were saved in a transaction that is still open (one begun outside an audited context and handed to it with `UseTransaction`): do not save them again. Write [`AuditStoreException.Entries`](tenantry-pro-efcore-auditstoreexception.md) somewhere else, or retry the store.

```csharp
public sealed class AuditStoreException : Exception, ISerializable
```

Inherits `Exception`.

Implements `ISerializable`.

## Constructors

### `AuditStoreException(IReadOnlyList<AuditEntry>, Exception)`

Initialises a new instance of [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md).

```csharp
public AuditStoreException(IReadOnlyList<AuditEntry> entries, Exception innerException)
```

Parameters:

- `entries` `IReadOnlyList<AuditEntry>`: The entries that were not written.
- `innerException` `Exception`: The store's failure, or an `AggregateException` of several.

## Properties

### `Entries`

The entries that were not written, in the order they were saved.

```csharp
public IReadOnlyList<AuditEntry> Entries { get; }
```

Value: `IReadOnlyList<AuditEntry>`
