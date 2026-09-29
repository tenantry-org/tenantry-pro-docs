# `HealthCheckBuilderExtensions` class

Namespace: `Tenantry.Pro.HealthChecks.Extensions` · Package: `Tenantry.Pro.HealthChecks` · [API reference](README.md)

Extension methods for adding Tenantry.Pro health checks to the ASP.NET Core health check pipeline.

```csharp
public static class HealthCheckBuilderExtensions
```

## Methods

### `AddTenantryDatabaseCheck<TKey>(IHealthChecksBuilder, Action<TenantHealthCheckOptions>)`

Adds a health check that verifies all tenant databases are reachable. Reports per-tenant connectivity in the health check data dictionary.

```csharp
public static IHealthChecksBuilder AddTenantryDatabaseCheck<TKey>(this IHealthChecksBuilder builder, Action<TenantHealthCheckOptions> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `builder` `IHealthChecksBuilder`: The health checks builder.
- `configure` `Action<TenantHealthCheckOptions>`: Configure health check options. At minimum set [`TenantHealthCheckOptions.ConnectionFactory`](tenantry-pro-healthchecks-tenanthealthcheckoptions.md) to a delegate that creates a `DbConnection` for your database provider.

Returns: `IHealthChecksBuilder`: The same builder for chaining.

```csharp
builder.Services.AddHealthChecks()
    .AddTenantryDatabaseCheck<Guid>(opts =>
        opts.ConnectionFactory = connStr => new SqlConnection(connStr));
```

### `AddTenantryMigrationCheck<TKey, TContext>(IHealthChecksBuilder, Func<string, TContext>)`

Adds a health check that verifies all tenant databases have current EF Core migrations. Reports pending migration names per tenant in the health check data dictionary.

```csharp
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not AOT-safe.")]
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe.")]
public static IHealthChecksBuilder AddTenantryMigrationCheck<TKey, TContext>(this IHealthChecksBuilder builder, Func<string, TContext> contextFactory) where TKey : IEquatable<TKey>, IParsable<TKey> where TContext : DbContext
```

Type parameters:

- `TKey`: The tenant identifier type.
- `TContext`: The consumer's EF Core `DbContext` type.

Parameters:

- `builder` `IHealthChecksBuilder`: The health checks builder.
- `contextFactory` `Func<string, TContext>`: A factory that accepts a per-tenant connection string and returns a configured `TContext` to query pending migrations from.

Returns: `IHealthChecksBuilder`: The same builder for chaining.
