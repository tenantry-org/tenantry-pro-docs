# `AuditOptions` class

Namespace: `Tenantry.Pro.EfCore.Audit` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Options for the Tenantry.Pro audit-logging feature. Configure via `pro.AddAuditLogging(opts => { ... })`.

```csharp
public sealed class AuditOptions
```

## Properties

### `ExcludeTypes`

Entity CLR types to exclude from audit logging. Useful for read-only projections, outbox records, or high-volume tables where change tracking is not meaningful.

```csharp
public ISet<Type> ExcludeTypes { get; set; }
```

Value: `ISet<Type>`

```csharp
opts.ExcludeTypes.Add(typeof(OutboxMessage));
```
