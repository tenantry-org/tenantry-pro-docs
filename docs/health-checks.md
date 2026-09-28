# Health checks

`Tenantry.Pro.HealthChecks` adds ASP.NET Core health checks that probe **every** tenant database: one
verifies connectivity, the other reports pending EF Core migrations. Both enumerate tenants from your
store and report per-tenant detail in the health check data dictionary.

## Install

```bash
dotnet add package Tenantry.Pro.HealthChecks
```

Both checks build on database-per-tenant, so `pro.UseDatabasePerTenant(...)` must be configured with a
`GetConnectionString` delegate.

## Database connectivity check

`AddTenantryDatabaseCheck<TKey>` opens an ADO.NET connection to each tenant's database. Supply a
`ConnectionFactory` that turns a connection string into the right `DbConnection` for your provider:

```csharp
using Microsoft.Data.SqlClient;
using Tenantry.Pro.HealthChecks.Extensions;

builder.Services.AddHealthChecks()
    .AddTenantryDatabaseCheck<string>(opts =>
    {
        opts.ConnectionFactory = cs => new SqlConnection(cs);     // Npgsql: new NpgsqlConnection(cs)
        opts.ConnectionTimeout = TimeSpan.FromSeconds(5);          // per-tenant open timeout (default 5s)
        opts.Tags = ["tenantry", "database"];                      // health check tags
    });
```

Result semantics:

| Condition | Status |
|-----------|--------|
| All tenant databases reachable | **Healthy** |
| One or more unreachable | **Unhealthy** (data lists which tenants failed and why) |
| No tenants registered | **Healthy** ("No tenants registered.") |
| `ConnectionFactory` / `GetConnectionString` not configured | **Degraded** (a configuration problem, not a tenant outage) |

The check is registered under the name `tenantry-databases`.

## Migration check

`AddTenantryMigrationCheck<TKey, TContext>` queries each tenant's pending migrations. Supply a factory
that builds your `DbContext` from a connection string:

```csharp
builder.Services.AddHealthChecks()
    .AddTenantryMigrationCheck<string, AppDbContext>(
        cs => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options));
```

Result semantics:

| Condition | Status |
|-----------|--------|
| All tenant databases up to date | **Healthy** |
| One or more have pending migrations | **Degraded** (data lists pending migration names per tenant) |
| Error querying a tenant | counted as needing attention; reported in data |

Registered under the name `tenantry-migrations`. Because it inspects EF Core migration metadata, it
uses reflection and is annotated `[RequiresDynamicCode]`/`[RequiresUnreferencedCode]` — expect the
AOT/trimming analyzer to flag it.

## Exposing the endpoint

```csharp
var app = builder.Build();

app.MapHealthChecks("/health");

// Or split readiness from liveness using the tags above:
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("tenantry")
});
```

A `GET /health` returns the aggregate status; enable a detailed response writer to surface the
per-tenant data dictionary if you want tenant-level visibility.

## See also

- [Database per tenant](database-per-tenant.md) — the connection-string source these checks use.
- [Migration orchestration](migration-orchestration.md) — the same pending-migration information, plus the ability to apply it.
