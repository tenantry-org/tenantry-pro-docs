# `TenantIsolation` enum

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Where a tenant's data lives, in mixed mode ([`MixedModeOptions<TKey>.GetIsolation`](tenantry-pro-mixedmodeoptions.md)).

```csharp
public enum TenantIsolation
```

## Values

| Value | Description |
|-------|-------------|
| `Shared = 0` | In the shared database and schema, kept apart from other tenants' rows only by Tenantry's query filters. Every entity its contexts use must implement `ITenantEntity<TKey>` or be marked as shared with `[SharedAcrossTenants]`; Tenantry.Pro.EfCore refuses a context with another. |
| `Schema = 1` | In its own schema of a shared database. |
| `Database = 2` | In its own database. |
