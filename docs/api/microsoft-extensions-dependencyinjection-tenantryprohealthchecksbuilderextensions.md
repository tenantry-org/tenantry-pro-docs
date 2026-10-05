# `TenantryProHealthChecksBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Adds Tenantry.Pro's tenant health checks to ASP.NET Core's (or any host's) health checks.

```csharp
public static class TenantryProHealthChecksBuilderExtensions
```

## Methods

### `AddTenantDatabaseCheck<TContext>(IHealthChecksBuilder, string?, HealthStatus?, IEnumerable<string>?, TimeSpan?, Action<TenantHealthCheckOptions>?)`

Adds a health check that every tenant's database is reachable: it opens a connection through `TContext`, created in each tenant's scope as the application registers it, once for each distinct database. The data has an entry for each tenant (`tenant:{id}`, the id formatted with the invariant culture).

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IHealthChecksBuilder AddTenantDatabaseCheck<TContext>(this IHealthChecksBuilder builder, string? name = null, HealthStatus? failureStatus = null, IEnumerable<string>? tags = null, TimeSpan? timeout = null, Action<TenantHealthCheckOptions>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context that connects to each tenant's database.

Parameters:

- `builder` `IHealthChecksBuilder`: The health checks builder.
- `name` `string`: The check's name. Default: `tenant-databases`.
- `failureStatus` `HealthStatus?`: What the check reports when a database is unreachable. Default: `Degraded`.
- `tags` `IEnumerable<string>`: Tags to select the check by. Default: `tenantry` and `database`.
- `timeout` `TimeSpan?`: How long the whole check may take. Default: 30 seconds.
- `configure` `Action<TenantHealthCheckOptions>`: Optionally sets how many databases are checked at once, how long each may take, and how long a result is kept.

Returns: `IHealthChecksBuilder`: The same `builder` for chaining.

For monitoring, not for liveness or readiness probes: one unreachable tenant database would fail the check on every replica at once, which is why it reports Degraded (ASP.NET Core answers Degraded with 200 unless the endpoint maps it otherwise). The data names tenants and gives database errors, so serve it only on a protected endpoint. It needs Tenantry (`AddTenantry`); without it the check reports its failure status and says so.

```csharp
builder.Services.AddHealthChecks().AddTenantDatabaseCheck<AppDbContext>();
```

### `AddTenantMigrationCheck<TContext>(IHealthChecksBuilder, string?, HealthStatus?, IEnumerable<string>?, TimeSpan?, Action<TenantHealthCheckOptions>?)`

Adds a health check that every tenant's database or schema has all of `TContext`'s migrations applied, read once for each distinct database and schema. The data has an entry for each tenant (`tenant:{id}`, the id formatted with the invariant culture): up to date, the pending migrations, or why they could not be read.

```csharp
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
public static IHealthChecksBuilder AddTenantMigrationCheck<TContext>(this IHealthChecksBuilder builder, string? name = null, HealthStatus? failureStatus = null, IEnumerable<string>? tags = null, TimeSpan? timeout = null, Action<TenantHealthCheckOptions>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context whose migrations are checked.

Parameters:

- `builder` `IHealthChecksBuilder`: The health checks builder.
- `name` `string`: The check's name. Default: `tenant-migrations`.
- `failureStatus` `HealthStatus?`: What the check reports when migrations are pending or cannot be read. Default: `Degraded`.
- `tags` `IEnumerable<string>`: Tags to select the check by. Default: `tenantry` and `migrations`.
- `timeout` `TimeSpan?`: How long the whole check may take. Default: 30 seconds.
- `configure` `Action<TenantHealthCheckOptions>`: Optionally sets how many databases are read at once, how long each may take, and how long a result is kept.

Returns: `IHealthChecksBuilder`: The same `builder` for chaining.

The context is created as [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md) creates it: as `pro.AddMigrations<TContext>()` configures it, if it was called, otherwise as the application registers it. For monitoring, not for liveness or readiness probes; the data names tenants, migrations and database errors, so serve it only on a protected endpoint. It needs Tenantry (`AddTenantry`); without it the check reports its failure status and says so.
