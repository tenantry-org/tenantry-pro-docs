# `TenantHealthCheckOptions` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

How a tenant health check (`AddTenantDatabaseCheck`, `AddTenantMigrationCheck`) checks the tenants' databases. Set with its `configure` argument.

```csharp
public sealed class TenantHealthCheckOptions
```

## Properties

### `CacheDuration`

How long the check reports its last result without checking again. Default: 30 seconds; zero checks every time.

```csharp
public TimeSpan CacheDuration { get; set; }
```

Value: `TimeSpan`

A check reads every tenant's database, so a monitoring system that polls often would otherwise put that load on each of them at each poll. Concurrent polls share one check.

### `DatabaseTimeout`

How long the check waits for one database (or schema) before counting it as failed. Default: 5 seconds.

```csharp
public TimeSpan DatabaseTimeout { get; set; }
```

Value: `TimeSpan`

### `MaxConcurrency`

How many databases (or schemas) the check reads at the same time. Default: 8.

```csharp
public int MaxConcurrency { get; set; }
```

Value: `int`

The first tenant is checked alone (until a tenant's context has been created), then the others this many at a time: some EF Core providers set up shared state the first time a context is used, without a lock. So with every database unreachable, a check takes about (1 + (database count − 1) ÷ [`TenantHealthCheckOptions.MaxConcurrency`](tenantry-pro-efcore-tenanthealthcheckoptions.md)) × [`TenantHealthCheckOptions.DatabaseTimeout`](tenantry-pro-efcore-tenanthealthcheckoptions.md), unless the check's own timeout stops it first.
