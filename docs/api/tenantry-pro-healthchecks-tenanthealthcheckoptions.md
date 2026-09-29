# `TenantHealthCheckOptions` class

Namespace: `Tenantry.Pro.HealthChecks` · Package: `Tenantry.Pro.HealthChecks` · [API reference](README.md)

Options for tenant database health checks. Configure via `builder.Services.AddHealthChecks().AddTenantryDatabaseCheck<TKey>(opts => { ... })`.

```csharp
public sealed class TenantHealthCheckOptions
```

## Properties

### `ConnectionFactory`

Factory that creates an ADO.NET `DbConnection` from a connection string. Required for `AddTenantryDatabaseCheck`.

```csharp
public Func<string, DbConnection>? ConnectionFactory { get; set; }
```

Value: `Func<string, DbConnection>`

```csharp
opts.ConnectionFactory = connStr => new SqlConnection(connStr);
opts.ConnectionFactory = connStr => new NpgsqlConnection(connStr);
```

### `ConnectionTimeout`

Maximum time to wait for each tenant's database connection to open. Default: 5 seconds.

```csharp
public TimeSpan ConnectionTimeout { get; set; }
```

Value: `TimeSpan`

### `Tags`

ASP.NET Core health check tags applied to the database connectivity check. Useful for filtering in readiness vs. liveness probes. Default: `["tenantry", "database"]`.

```csharp
public IList<string> Tags { get; set; }
```

Value: `IList<string>`
