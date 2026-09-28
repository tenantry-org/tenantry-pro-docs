# Audit Logging

Use this package to record EF Core entity changes (insert, update, delete) with the current
tenant ID, timestamps, and changed property values.

## What Tenantry.Pro.EfCore Provides

- `TenantAuditInterceptor<TKey>` — EF Core `SaveChangesInterceptor` that records changes
- `IAuditStore` — interface for persisting or dispatching audit entries
- Default `IAuditStore` implementation that writes structured log entries via `ILogger`
- `AuditEntry` — immutable record with table, action, PK, old/new values, tenant ID, timestamp
- `pro.AddAuditLogging(opts => ...)` — registers the interceptor and default store in DI
- `options.UseAuditLogging<TKey>(sp)` — wires the interceptor into a `DbContext`

## Requirements

- `Tenantry.Pro.EfCore` NuGet package
- Any EF Core provider (not SqlServer-specific)

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro;
using Tenantry.Pro.EfCore;
using Tenantry.Pro.EfCore.Extensions;

// 1. Register Tenantry with audit logging
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddAuditLogging(opts =>
        {
            // Optionally exclude entity types from audit:
            opts.ExcludeTypes.Add(typeof(OutboxMessage));
        });
    });
});

// 2. Wire the interceptor into your DbContext
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")!);
    options.UseAuditLogging<string>(sp);   // adds TenantAuditInterceptor<TKey>
});
```

## Providing a custom audit store

The default store logs each audit entry as a structured `Information` log message.
To persist entries to a database, event stream, or external audit service, implement
`IAuditStore` and register it after `pro.AddAuditLogging()`:

```csharp
builder.Services.AddSingleton<IAuditStore, MyDatabaseAuditStore>();
```

```csharp
public sealed class MyDatabaseAuditStore(AuditDbContext db) : IAuditStore
{
    public async Task SaveAsync(
        IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        foreach (var entry in entries)
        {
            db.AuditLogs.Add(new AuditLog
            {
                TableName  = entry.TableName,
                Action     = entry.Action.ToString(),
                PrimaryKey = entry.PrimaryKey,
                TenantId   = entry.TenantId,
                Timestamp  = entry.Timestamp,
                OldValues  = JsonSerializer.Serialize(entry.OldValues),
                NewValues  = JsonSerializer.Serialize(entry.NewValues),
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

## AuditEntry reference

| Property | Description |
|----------|-------------|
| `TableName` | EF Core table name (or CLR type name as fallback) |
| `Action` | `Insert`, `Update`, or `Delete` |
| `PrimaryKey` | Comma-separated PK value(s); `null` if no key defined |
| `OldValues` | Property values before the change (empty for inserts) |
| `NewValues` | Property values after the change (empty for deletes) |
| `ChangedProperties` | Names of properties that changed |
| `Timestamp` | UTC time the entry was recorded |
| `TenantId` | Current tenant ID; `null` when no tenant scope is active |

## Limitations

- Audit entries are recorded after a **successful** `SaveChanges` call. Changes that fail
  and are rolled back are not audited.
- EF Core's `ChangeTracker` uses reflection; this interceptor is not AOT or trim compatible.
  The analyzer will warn at build time — this is expected for migration and audit features.
- If `IAuditStore.SaveAsync` throws, the exception is caught and logged. The original
  `SaveChanges` result is returned normally — audit failures are non-fatal.

## See also

- [Background jobs & non-HTTP hosts](background-jobs.md) — `AuditEntry.TenantId` is `null` for work
  that runs without a tenant scope; open a scope first if you want jobs audited under a tenant.
- [Troubleshooting](troubleshooting.md) — the AOT/trimming analyzer warnings are expected here.
