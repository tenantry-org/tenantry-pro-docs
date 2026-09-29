# `AuditEntry` class

Namespace: `Tenantry.Pro.EfCore.Audit` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Represents a single audited entity change captured before an EF Core `SaveChanges` call.

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

Value: [`AuditAction`](tenantry-pro-efcore-audit-auditaction.md)

### `ChangedProperties`

The names of properties that changed. For inserts and deletes this is all tracked properties; for updates it is only the modified properties.

```csharp
public required IReadOnlySet<string> ChangedProperties { get; init; }
```

Value: `IReadOnlySet<string>`

### `NewValues`

Property values after the change. Empty for [`AuditAction.Delete`](tenantry-pro-efcore-audit-auditaction.md); populated for [`AuditAction.Insert`](tenantry-pro-efcore-audit-auditaction.md) (all properties) and [`AuditAction.Update`](tenantry-pro-efcore-audit-auditaction.md) (changed properties only).

```csharp
public required IReadOnlyDictionary<string, object?> NewValues { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`

### `OldValues`

Property values before the change. Empty for [`AuditAction.Insert`](tenantry-pro-efcore-audit-auditaction.md); populated for [`AuditAction.Update`](tenantry-pro-efcore-audit-auditaction.md) (changed properties only) and [`AuditAction.Delete`](tenantry-pro-efcore-audit-auditaction.md) (all properties).

```csharp
public required IReadOnlyDictionary<string, object?> OldValues { get; init; }
```

Value: `IReadOnlyDictionary<string, object>`

### `PrimaryKey`

A string representation of the entity's primary key value(s). Composite keys are comma-separated. [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when no primary key is defined.

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

The current tenant identifier, serialised as a string. [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when no tenant scope is active (e.g. admin background jobs).

```csharp
public required string? TenantId { get; init; }
```

Value: `string`

### `Timestamp`

The UTC timestamp at which the audit entry was recorded.

```csharp
public required DateTimeOffset Timestamp { get; init; }
```

Value: `DateTimeOffset`
