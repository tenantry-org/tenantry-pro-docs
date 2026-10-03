# `AuditEntry` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

One entity change an EF Core `SaveChanges` call saved, recorded by audit logging (`pro.AddAuditLogging()`).

```csharp
public sealed record AuditEntry : IEquatable<AuditEntry>
```

Implements `IEquatable<AuditEntry>`.

## Properties

### `Action`

The type of change: insert, update, or delete.

```csharp
public required AuditAction Action { get; init; }
```

Value: [`AuditAction`](tenantry-pro-efcore-auditaction.md)

### `Actor`

The user or service that made the change, as [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) gives it: [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) unless you register one that names it.

```csharp
public string? Actor { get; init; }
```

Value: `string`

### `ChangedProperties`

The names of properties that changed, as [`AuditEntry.OldValues`](tenantry-pro-efcore-auditentry.md) and [`AuditEntry.NewValues`](tenantry-pro-efcore-auditentry.md) name them. For inserts and deletes this is all tracked properties; for updates it is only the modified properties.

```csharp
public required IReadOnlySet<string> ChangedProperties { get; init; }
```

Value: `IReadOnlySet<string>`

Properties excluded with [`AuditOptions.ExcludeProperty<TEntity>`](tenantry-pro-efcore-auditoptions.md) are not in it, nor in [`AuditEntry.OldValues`](tenantry-pro-efcore-auditentry.md) and [`AuditEntry.NewValues`](tenantry-pro-efcore-auditentry.md).

### `CorrelationId`

An id that ties the change to the request, message or job that made it, as [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) gives it: by default the current trace's id, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when there is no trace.

```csharp
public string? CorrelationId { get; init; }
```

Value: `string`

### `Data`

Values of your own, from [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md). Empty by default.

```csharp
public IReadOnlyDictionary<string, object?> Data { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`

### `EntityType`

EF Core's name for the entity type (`IEntityType.Name`): the full name of its CLR type, such as `MyApp.Orders.Order`, or for a shared-type entity (a many-to-many join, a property bag) the name it is mapped with. An owned type's name also names its owner and navigation.

```csharp
public required string EntityType { get; init; }
```

Value: `string`

### `NewValues`

Property values after the change. Empty for [`AuditAction.Delete`](tenantry-pro-efcore-auditaction.md); populated for [`AuditAction.Insert`](tenantry-pro-efcore-auditaction.md) (all properties) and [`AuditAction.Update`](tenantry-pro-efcore-auditaction.md) (changed properties only).

```csharp
public required IReadOnlyDictionary<string, object?> NewValues { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`

Named as in [`AuditEntry.OldValues`](tenantry-pro-efcore-auditentry.md).

### `OldValues`

Property values before the change. Empty for [`AuditAction.Insert`](tenantry-pro-efcore-auditaction.md); populated for [`AuditAction.Update`](tenantry-pro-efcore-auditaction.md) (changed properties only) and [`AuditAction.Delete`](tenantry-pro-efcore-auditaction.md) (all properties).

```csharp
public required IReadOnlyDictionary<string, object?> OldValues { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`

A property of a complex type (`ComplexProperty`) is named by its path, as `Address.City`, and an optional complex property that is null by its own path, with a null value. A complex collection (`ComplexCollection`, EF Core 10) is recorded whole when it changes: a list with one dictionary of values, named the same way, for each element. Values are copies taken before the save, so changing the entity afterwards does not change them.

### `PrimaryKey`

A string representation of the entity's primary key value(s), each in the invariant culture: a date or time to the tick (`2026-10-02T13:04:05.1230000`), a byte array in hexadecimal (`0x0102`), and a value-converted key that cannot be formatted so, such as a strongly typed id, as the value it is stored as. Composite keys are comma-separated. [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when no primary key is defined.

```csharp
public required string? PrimaryKey { get; init; }
```

Value: `string`

### `TableName`

The database table name mapped by the entity's EF Core metadata. Falls back to the entity CLR type name when the provider has no table mapping.

```csharp
public required string TableName { get; init; }
```

Value: `string`

### `TenantId`

The tenant current when the change was saved, formatted with the invariant culture, as jobs, messages and traces carry it. [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when no tenant scope is active (e.g. admin background jobs).

```csharp
public required string? TenantId { get; init; }
```

Value: `string`

### `Timestamp`

The UTC time at which `SaveChanges` saved the change.

```csharp
public required DateTimeOffset Timestamp { get; init; }
```

Value: `DateTimeOffset`
