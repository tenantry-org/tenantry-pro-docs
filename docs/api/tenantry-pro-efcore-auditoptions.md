# `AuditOptions` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Options for the Tenantry.Pro audit-logging feature. Configure via `pro.AddAuditLogging(opts => { ... })`.

```csharp
public sealed class AuditOptions
```

## Properties

### `OnStoreFailure`

What happens when the store fails to write the entries of changes that are already saved, with [`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md): the failure is logged ([`AuditStoreFailureBehavior.Log`](tenantry-pro-efcore-auditstorefailurebehavior.md), the default) or thrown as an [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md) ([`AuditStoreFailureBehavior.Throw`](tenantry-pro-efcore-auditstorefailurebehavior.md)). With [`AuditTiming.InTransaction`](tenantry-pro-efcore-audittiming.md) the store's failure is always thrown, as it is.

```csharp
public AuditStoreFailureBehavior OnStoreFailure { get; set; }
```

Value: [`AuditStoreFailureBehavior`](tenantry-pro-efcore-auditstorefailurebehavior.md)

### `ShouldAudit`

Decides, for each entity a save inserts, updates or deletes, whether to record it: return [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) to leave it out. Called before its values are read, after the exclusions of [`AuditOptions.Exclude<TEntity>`](tenantry-pro-efcore-auditoptions.md). [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) (the default) records every entity not excluded.

```csharp
public Func<EntityEntry, bool>? ShouldAudit { get; set; }
```

Value: `Func<EntityEntry, bool>`

An owned entity is an entity of its own here, asked about on its own (`entry.Metadata.IsOwned()`): leaving out its owner does not leave it out. [`AuditOptions.Exclude<TEntity>`](tenantry-pro-efcore-auditoptions.md) leaves out what an excluded type owns.

```csharp
opts.ShouldAudit = entry => entry.Metadata.GetTableName() != "Sessions";
```

### `Timing`

When a save's entries reach [`IAuditStore`](tenantry-pro-efcore-iauditstore.md): once the changes are committed ([`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md), the default), or inside the transaction they were saved in ([`AuditTiming.InTransaction`](tenantry-pro-efcore-audittiming.md)).

```csharp
public AuditTiming Timing { get; set; }
```

Value: [`AuditTiming`](tenantry-pro-efcore-audittiming.md)

## Methods

### `ExcludeProperty<TEntity>(Expression<Func<TEntity, object?>>)`

Leaves a property out of every entry of `TEntity`, and of types derived from it: its value is not copied, and it is not in [`AuditEntry.OldValues`](tenantry-pro-efcore-auditentry.md), [`AuditEntry.NewValues`](tenantry-pro-efcore-auditentry.md) or [`AuditEntry.ChangedProperties`](tenantry-pro-efcore-auditentry.md). A key property still makes up [`AuditEntry.PrimaryKey`](tenantry-pro-efcore-auditentry.md). An update that changes only excluded properties is not recorded. Naming the navigation to an owned type (`OwnsOne`, `OwnsMany`) leaves out the entities owned through it, and naming a complex property (`ComplexProperty`, `ComplexCollection`) leaves out its values.

```csharp
public AuditOptions ExcludeProperty<TEntity>(Expression<Func<TEntity, object?>> property)
```

Type parameters:

- `TEntity`: The entity type, complex type, base type or interface that has the property.

Parameters:

- `property` `Expression<Func<TEntity, object>>`: The property, as `x => x.PasswordHash`, or a navigation to an owned type, or a complex property. For one property of an owned or complex type, name it on that type.

Returns: [`AuditOptions`](tenantry-pro-efcore-auditoptions.md): The same options, for chaining.

Exceptions:

- `ArgumentException`: `property` is not a member of `TEntity` itself (a nested member, a method call).

```csharp
opts.ExcludeProperty<User>(user => user.PasswordHash);
```

### `Exclude<TEntity>()`

Leaves `TEntity` out of the audit: every entity of that type, of a type derived from it (or implementing it, for an interface), and the entities they own (`OwnsOne`, `OwnsMany`).

```csharp
public AuditOptions Exclude<TEntity>() where TEntity : class
```

Type parameters:

- `TEntity`: The entity type, base type or interface to exclude.

Returns: [`AuditOptions`](tenantry-pro-efcore-auditoptions.md): The same options, for chaining.

```csharp
opts.Exclude<OutboxMessage>();
```
