# `AuditAction` enum

Namespace: `Tenantry.Pro.EfCore.Audit` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

The type of data-modification event captured by an audit entry.

```csharp
public enum AuditAction
```

## Values

| Value | Description |
|-------|-------------|
| `Insert = 0` | A new entity row was inserted. |
| `Update = 1` | An existing entity row was updated. |
| `Delete = 2` | An existing entity row was deleted. |
