# Health checks

`Tenantry.Pro.EfCore` adds ASP.NET Core health checks that probe every tenant database: one verifies
connectivity, the other reports pending EF Core migrations. Both go through your own `DbContext`, created in each
tenant's scope as your application creates it, read each database (or schema) once however many tenants share it,
and report per-tenant detail in the health check data dictionary.

> Use these checks for monitoring, not for liveness or readiness: one unreachable tenant database fails the check on
> every replica at once. The checks report Degraded, which ASP.NET Core returns as `200`, so map Degraded to `503` on a separate,
> protected endpoint ([below](#exposing-the-checks)).

## Registration

```csharp
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

builder.Services.AddHealthChecks()
    .AddTenantDatabaseCheck<AppDbContext>()      // every tenant database is reachable
    .AddTenantMigrationCheck<AppDbContext>();    // every tenant database or schema has every migration applied
```

The checks need Tenantry (`AddTenantry`); without it they report their failure status, saying so. They get the
context from each tenant's scope, as a request does (or from its `IDbContextFactory<TContext>` when it cannot be
created there; see [Tenant migrations](migration-orchestration.md#registration)), so register it so that a tenant's
scope gives the tenant's own database: Tenantry core's `AddDbContextPerTenantDatabase`, or a context with
`UseTenantry()` and schema per tenant. Both take the standard health check arguments, and options:

```csharp
builder.Services.AddHealthChecks()
    .AddTenantDatabaseCheck<AppDbContext>(
        name: "tenant-databases",                          // the default
        failureStatus: HealthStatus.Degraded,              // the default
        tags: ["tenantry", "database"],                    // the default
        timeout: TimeSpan.FromSeconds(30),                 // the whole check (default 30 s)
        configure: o =>
        {
            o.MaxConcurrency = 8;                          // databases checked at once (default 8)
            o.DatabaseTimeout = TimeSpan.FromSeconds(5);    // per database, creating its context included (default 5 s)
            o.CacheDuration = TimeSpan.FromSeconds(30);     // how long a result is reported (default 30 s)
        });
```

## Database connectivity check

`AddTenantDatabaseCheck<TContext>` opens a connection through `TContext` for each tenant, once for each distinct
database (tenants whose context has the same connection string share it, whatever their schema):

| Condition | Status |
|-----------|--------|
| All tenant databases reachable | Healthy |
| One or more unreachable | `failureStatus`, Degraded by default (data lists which tenants failed and why) |
| No tenants registered | Healthy ("No tenants.") |

Each tenant's entry in the data (`tenant:{id}`, the id formatted with the invariant culture) is `reachable`, or
`unreachable:` and the provider's error. A tenant whose context cannot be created, because its connection string cannot be read, say, counts as unreachable.

## Migration check

`AddTenantMigrationCheck<TContext>` reads each distinct database's or schema's pending migrations, grouped and
connected as the [migration runner](migration-orchestration.md#running-migrations) does: through `AddMigrations`'
`CreateContext` if you set it, otherwise through your registered context.

| Condition | Status |
|-----------|--------|
| All tenant databases or schemas up to date | Healthy |
| One or more have pending migrations, or could not be read | `failureStatus`, Degraded by default |

Each tenant's entry in the data is `up to date`, the number and names of its pending migrations, or `error:` and why
they could not be read. The check is named `tenant-migrations` and tagged `tenantry` and `migrations` by default.

## What the checks cover

- They cover every tenant the store lists, suspended or still provisioning ones included
  ([why](migration-orchestration.md#which-tenants-are-migrated)). A tenant whose database does not exist yet shows as
  unreachable, with every migration pending.
- Each check keeps its result for `CacheDuration` (30 seconds by default),
  and polls that arrive while it runs wait for that run, so a monitor polling every few seconds does not reach every
  tenant database each time. Set it to zero to check on every request.
- Each check reads the first tenant's database alone, then up to `MaxConcurrency` at a
  time, and waits up to `DatabaseTimeout` for each. With every database unreachable a check takes about
  (1 + (databases − 1) ÷ `MaxConcurrency`) × `DatabaseTimeout`; the check's `timeout` (30 seconds by default) ends it
  sooner, as a failure. The two checks run at once. Allow for your tenant count in the monitor's timeout.

## Exposing the checks

Keep liveness and readiness to checks about the process itself, and put the tenant checks on their own
endpoint:

```csharp
var app = builder.Build();

// Liveness: the process is running. Runs no checks.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Readiness: only your own process-level checks, tagged "ready". Never the tenant checks.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Monitoring: every tenant's status, for your monitoring system only.
app.MapHealthChecks("/health/tenants", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("tenantry"),
    ResultStatusCodes = { [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable }
}).RequireAuthorization("HealthMonitoring");   // a policy you register with AddAuthorization
```

Do not tag the tenant checks `ready`. If you call `RequireTenantByDefault()`, add
`.AllowMissingTenant()` to each endpoint, or requests without a tenant get `400`. On ASP.NET Core 9 and later,
`.DisableHttpMetrics()` keeps an endpoint out of ASP.NET Core's request metrics.

### Protect the monitoring endpoint

The per-tenant data names every tenant (`tenant:{TenantId}`) and carries the provider's error message for
each failure, which can include host names, database names and login names; the migration check adds
pending migration names. The default response writer returns only the overall status, but a detailed
writer, such as one that feeds a dashboard, returns all of it. (`UIResponseWriter.WriteHealthCheckUIResponseNoExceptionDetails`
from AspNetCore.HealthChecks.UI.Client does not hide it: the text is in the data, not in the exception.)
Even with the default writer, a request after the cached result expires opens a connection to every tenant
database.

So require authorization on the endpoint, as above, and keep it off the public internet: block
`/health/tenants` at your ingress or load balancer. Listening on a second, internal port is not enough on
its own, because Kestrel serves every endpoint on every port it listens on; if you use one, check the port
the request arrived on in your authorization policy, for example
`policy.RequireAssertion(c => c.Resource is HttpContext http && http.Connection.LocalPort == 8081)`.
`RequireHost` is not access control, because it trusts the request's `Host` header.

To keep the checks off HTTP altogether, run them on a schedule instead: an `IHealthCheckPublisher`, with
`HealthCheckPublisherOptions.Predicate` selecting the `tenantry` tag, receives each report and can send it
to your monitoring. Set `HealthCheckPublisherOptions.Timeout` (30 seconds by default) above the checks'
worst case (see [What the checks cover](#what-the-checks-cover)): a run that exceeds it is cancelled and
nothing is published. Alert when reports stop arriving, and run the publisher on one instance, since each
instance would otherwise probe every tenant database.

## See also

- [Database per tenant](database-per-tenant.md): the per-tenant context these checks use.
- [Tenant migrations](migration-orchestration.md): the same pending migrations, and applying them.
