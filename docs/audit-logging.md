# Audit Logging

Record every insert, update and delete your EF Core contexts save, with the tenant, the time, who made the change and
the changed values.
`pro.AddAuditLogging()` audits every context that uses `UseTenantry()`, and passes the entries to an `IAuditStore` once
the changes are committed.

## What it provides

- `pro.AddAuditLogging(o => …)`: audits every context that uses `UseTenantry()`, with nothing added to the contexts.
- `IAuditStore`: receives the entries, to persist or dispatch them. The default store writes each entry to the log.
- `AuditEntry`: the table and entity type, the action, the primary key, old and new values, the tenant, the time,
  who made the change and a correlation id.
- `IAuditContextProvider`: says who made the change. By default no one, with the current trace's id as the correlation
  id.
- `AuditOptions`: entity types and properties to leave out (`Exclude<T>()`, `ExcludeProperty<T>(…)`, `ShouldAudit`),
  when entries are written (`Timing`), and what a store failure does (`OnStoreFailure`).

## Requirements

- A reference to `Tenantry.Pro.EfCore`, and any EF Core provider.
- Contexts that use Tenantry core's `UseTenantry()`, as with `AddDbContextPerTenantDatabase`. A context that does
  not is not audited.

## Registration

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro.EfCore;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddAuditLogging(opts =>
    {
        // Optionally leave entity types, or properties, out of the audit:
        opts.Exclude<OutboxMessage>();
        opts.ExcludeProperty<User>(user => user.PasswordHash);
    })));

builder.Services.AddDbContext<AppDbContext>(options => options
    .UseSqlServer(builder.Configuration.GetConnectionString("AppDb")!)
    .UseTenantry());   // audits the context's saves
```

`UseTenantry()` adds the audit interceptor after Tenantry's own, so a new tenant-owned entity is recorded with the
`TenantId` Tenantry stamps on it. `AuditEntry.TenantId` is the tenant current when the change was saved.

## Who made the change

Each entry has an `Actor`, the user or service that made the change, and a `CorrelationId` that ties it to the
request, message or job. They come from an `IAuditContextProvider`, which the audit asks once for each save that has
changes to record, before the changes are sent. The default names no actor and uses the current trace's id
(`Activity.Current`). ASP.NET Core sets one for each request; message consumers and background jobs have one when
tracing, such as OpenTelemetry, is enabled, and otherwise no correlation id. To name the user, register a provider of
your own, with any lifetime; in an ASP.NET Core application, from the request:

```csharp
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro.EfCore;

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditContextProvider, HttpAuditContextProvider>();

public sealed class HttpAuditContextProvider(IHttpContextAccessor http) : IAuditContextProvider
{
    public AuditContext GetContext(DbContext context) => new()
    {
        Actor = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
        CorrelationId = Activity.Current?.TraceId.ToHexString() ?? http.HttpContext?.TraceIdentifier,
        Data = new Dictionary<string, object?>
        {
            ["ClientIp"] = http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        },
    };
}
```

`Data` holds values of your own, recorded with each entry. A provider that throws stops the save before anything is
sent. For where it is resolved, see [Where the store and provider come from](#where-the-store-and-provider-come-from).

## Leaving things out

- `opts.Exclude<T>()` leaves out every entity of type `T`, of a type derived from it (or implementing it, when `T`
  is an interface), and the entities they own, whichever type the ownership is configured on.
- `opts.ExcludeProperty<T>(x => x.Property)` leaves a property out of every entry of `T` and its derived types:
  its value is never copied, and it is in none of `OldValues`, `NewValues` and `ChangedProperties` (a key property
  still makes up `PrimaryKey`). An update that changes only excluded properties is not recorded. Naming the
  navigation to an owned type, `opts.ExcludeProperty<User>(u => u.Credentials)`, leaves out the entities owned through
  it, and naming a complex property leaves out its values; for one property of an owned or complex type, name it on
  that type: `opts.ExcludeProperty<Address>(a => a.Street)`.
- `opts.ShouldAudit` decides for each entity whether to record it, from its `EntityEntry`, before its values are
  read: `opts.ShouldAudit = entry => entry.Metadata.GetTableName() != "Sessions";`. It is asked about each owned
  entity on its own (`entry.Metadata.IsOwned()`), so leaving out an owner does not leave out what it owns, as
  `Exclude<T>()` does.

## When entries are written

By default (`AuditTiming.AfterCommit`), the store gets a save's entries once its changes are committed:

- A save in no transaction of yours: when `SaveChanges` completes.
- In a transaction you begin (`Database.BeginTransaction`): when it commits, the entries of its saves in one call
  for each context. Contexts you hand it to (`Database.UseTransaction(transaction.GetDbTransaction())`) wait for it
  too, whichever context commits it. If it rolls back, or is disposed without a commit, the entries are discarded, as
  are those of a save that failed. Commit through the `IDbContextTransaction` (or `Database.CommitTransaction`):
  EF Core's interceptors do not see a commit made on the `DbTransaction` itself.
- Rolled back to a savepoint: the entries saved since the latest savepoint are discarded. EF Core does not tell
  interceptors which savepoint a transaction was rolled back to, so roll back only to the latest one. On SQL Server,
  which cannot release a savepoint, a released savepoint still counts.
- In a `TransactionScope`, or a transaction the connection was enlisted in (`Database.EnlistTransaction`): when it
  completes, and discarded if it is not completed. The store's writes are not part of that transaction. If EF Core's
  `AmbientTransactionWarning` is ignored for a provider that cannot enlist (SQLite), changes saved in a scope that is
  not completed are committed all the same, but their entries are discarded.
- In a transaction begun outside an audited context (with ADO.NET, say) and handed to the context with
  `Database.UseTransaction`: when `SaveChanges` completes, as Tenantry cannot see when that transaction commits. The
  store runs while it is still open, and the entries stay written if it rolls back.

### When the store fails

The changes are in the database by then (apart from that last case), so by default the
failure is logged as an error and the save, or the commit, succeeds; the store's token is never cancelled, for the
same reason. Set `o.OnStoreFailure = AuditStoreFailureBehavior.Throw` to have `SaveChanges`, or the commit, throw an
`AuditStoreException` instead, with the unwritten entries in `Entries`. The changes are saved all the same, so do not
save them again: write the entries somewhere else, or retry the store. A `TransactionScope`'s completion cannot throw
it, so there the failure is still logged.

```csharp
using Tenantry.Pro.EfCore;

try
{
    await db.SaveChangesAsync(ct);
}
catch (AuditStoreException exception)
{
    // The changes are committed; only their audit entries are missing.
    logger.LogCritical(exception, "{Count} audit entries were not written", exception.Entries.Count);
}
```

### Writing in the same transaction

To have audit rows commit or roll back with the changes they record, set `o.Timing = AuditTiming.InTransaction`.
The store is then called at the end of each `SaveChanges`, inside the transaction the changes were saved in, and
writes through the saving context's connection and transaction:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tenantry.Pro.EfCore;

tenant.UsePro(pro => pro.AddAuditLogging(o => o.Timing = AuditTiming.InTransaction));

builder.Services.AddDbContextFactory<AuditDbContext>(options => options.UseSqlServer());   // no connection: the store sets it
builder.Services.AddSingleton<IAuditStore, SameTransactionAuditStore>();

public sealed class SameTransactionAuditStore(IDbContextFactory<AuditDbContext> contexts) : IAuditStore
{
    public async Task SaveAsync(
        DbContext context, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        await using var audit = await contexts.CreateDbContextAsync(cancellationToken);
        audit.Database.SetDbConnection(context.Database.GetDbConnection());
        await audit.Database.UseTransactionAsync(
            context.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken);

        audit.AuditLogs.AddRange(entries.Select(entry => new AuditLog
        {
            TableName  = entry.TableName,
            Action     = entry.Action.ToString(),
            PrimaryKey = entry.PrimaryKey,
            TenantId   = entry.TenantId,
            Timestamp  = entry.Timestamp,
        }));
        await audit.SaveChangesAsync(cancellationToken);
    }
}
```

Save inside a transaction of your own (`Database.BeginTransaction`): without one, `SaveChanges` commits its changes
before the store is called, and the audit rows are written in a transaction of their own. A store failure, or
cancellation, is thrown from `SaveChanges`, so the transaction can be rolled back. By then the changes have been
sent in the transaction and the change tracker has accepted them, so roll back and discard the context. To retry on
the same context (with an execution strategy, say), save with `SaveChangesAsync(acceptAllChangesOnSuccess: false)` and
call `ChangeTracker.AcceptAllChanges()` once the transaction commits, as EF Core does for retries.

## Providing a custom audit store

The default store logs each audit entry as a structured `Information` log message.
To persist entries to a database, event stream, or external audit service, implement
`IAuditStore` and register it after `pro.AddAuditLogging()`.

It can be scoped and take scoped services, such as a `DbContext` of its own
([Where the store and provider come from](#where-the-store-and-provider-come-from)):

```csharp
builder.Services.AddDbContext<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")!));
builder.Services.AddScoped<IAuditStore, MyDatabaseAuditStore>();
```

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro.EfCore;

public sealed class MyDatabaseAuditStore(AuditDbContext db) : IAuditStore
{
    public async Task SaveAsync(
        DbContext context, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
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

`context` is the context that saved the changes. They are committed by the time this store is called, so do not
save through it. When a transaction commits after the context was disposed, or after a pooled context went back to
its pool, possibly to serve another tenant, read nothing from it but its type. The saves the store makes are not
audited, even through a context that uses `UseTenantry()`.

A singleton store that creates a context per call works too:

```csharp
builder.Services.AddDbContextFactory<AuditDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")!));
builder.Services.AddSingleton<IAuditStore, FactoryAuditStore>();
```

```csharp
using Microsoft.EntityFrameworkCore;
using Tenantry.Pro.EfCore;

public sealed class FactoryAuditStore(IDbContextFactory<AuditDbContext> contexts) : IAuditStore
{
    public async Task SaveAsync(
        DbContext context, IReadOnlyList<AuditEntry> entries, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        // add the entries as above
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

## Where the store and provider come from

Both are resolved for each save from the saving context's scope, so they can be scoped. A context without a scope of
its own (pooled, from an `IDbContextFactory`, or created by hand), or whose scope is gone by the time its transaction
commits, gets a new scope for the call, where scoped services are new: read ambient state, such as
`IHttpContextAccessor`, there. A transient store is disposed after each call.

## AuditEntry reference

| Property | Description |
|----------|-------------|
| `TableName` | EF Core table name (or CLR type name as fallback) |
| `EntityType` | EF Core's name for the entity type: its CLR type's full name, or the name a shared-type entity is mapped with |
| `Action` | `Insert`, `Update`, or `Delete` |
| `PrimaryKey` | Comma-separated PK value(s) in the invariant culture: a date to the tick (`2026-10-02T13:04:05.1230000`), a byte array in hexadecimal (`0x0102`), a strongly typed id as the value it is stored as; `null` if no key defined |
| `OldValues` | Property values before the change (empty for inserts) |
| `NewValues` | Property values after the change (empty for deletes) |
| `ChangedProperties` | Names of properties that changed |
| `Timestamp` | UTC time the change was saved |
| `TenantId` | The tenant current when the change was saved, formatted with the invariant culture; `null` when no tenant scope is active |
| `Actor` | Who made the change, from `IAuditContextProvider`; `null` by default |
| `CorrelationId` | The request, message or job that made it, from `IAuditContextProvider`; by default the current trace's id |
| `Data` | Values of your own, from `IAuditContextProvider`; empty by default |

A complex type's properties (`ComplexProperty`) are named by their path, as `Address.City`, and an optional complex
property that is null by its own path, with a null value. A complex collection (`ComplexCollection`, EF Core 10) is
recorded whole when it changes: a list with one dictionary of values for each element. The values are copies taken
when the save starts, as EF Core copies them to detect changes (byte arrays included), so changing the entity after
`SaveChanges`, before its transaction commits, does not change its entry. An update that changes only excluded
properties, including those of a complex collection's elements, is not recorded. EF Core does not track whether an
optional complex property or a complex collection was null before it was set, so an update that sets one that was
null records, as its old values, its properties' defaults or an empty collection.

## Limitations

- Only changes `SaveChanges` writes are recorded. `ExecuteUpdate`, `ExecuteDelete` and raw SQL (`ExecuteSql`,
  `ExecuteSqlRaw`) do not go through `SaveChanges`, so they produce no entries, nor do the rows offboarding's
  `DeleteSharedData` deletes: record them in your own code.
- A synchronous `SaveChanges` or `Commit`, and a `TransactionScope`'s completion, which is always synchronous,
  wait for the store, whose `SaveAsync` is asynchronous: prefer `SaveChangesAsync`. Use `ConfigureAwait(false)` in
  the store, so it does not need the caller's synchronization context: a scope completing on a UI thread (WPF,
  WinForms, MAUI) would otherwise wait for ever.
- Audit logging is not trim- or Native AOT-compatible
  ([Troubleshooting](troubleshooting.md#trimaot-analyzer-warnings-il2026-il3050)).

## See also

- [Background jobs & non-HTTP hosts](background-jobs.md): `AuditEntry.TenantId` is `null` for work that runs without
  a tenant scope; open a scope first if you want jobs audited under a tenant.
- [Troubleshooting](troubleshooting.md): the trimming and AOT analyzer warnings are expected here.
