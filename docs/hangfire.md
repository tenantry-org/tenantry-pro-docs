# Hangfire Integration

A Hangfire job enqueued while a tenant is current carries that tenant, and runs as it: the job, and the scoped
services Hangfire creates it with, see the tenant in `ITenantContext<TKey>`, as they would in a request. A job can
also be enqueued for a tenant by name, and a recurring job can run for each tenant. The
[HangfireJobs sample](../samples/Tenantry.Pro.Samples.HangfireJobs) runs a job enqueued during a request.

## Requirements

- `Tenantry.Pro.Hangfire` NuGet package
- Hangfire 1.8 or later 1.x. The package depends on `Hangfire.Core` only, so a worker service needs no ASP.NET Core.
- `Newtonsoft.Json` 13.0.1 or later, referenced by your application. Hangfire allows 11.0.1, which has a known
  vulnerability ([GHSA-5crp-9r3c-p9vr](https://github.com/advisories/GHSA-5crp-9r3c-p9vr)), so NuGet warns (NU1903)
  until something raises it. Tenantry.Pro.Hangfire does not use it, so it leaves the version to your application.

## Registration

```csharp
using Hangfire;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddHangfirePropagation()));

// AddHangfire's (sp, config) overload is Hangfire.NetCore's (Hangfire.AspNetCore includes it).
builder.Services.AddHangfire((sp, config) => config
    .UseInMemoryStorage()   // Hangfire.InMemory, or your storage
    .UseTenantry(sp));
builder.Services.AddHangfireServer();
```

`pro.AddHangfirePropagation()` registers the integration; `config.UseTenantry(sp)` adds Tenantry's job filter to
Hangfire's global filters. Without Hangfire.NetCore, call `GlobalConfiguration.Configuration.UseTenantry(app.Services)`
once the application is built, before it starts.

Hangfire's filters are global to the process, so `UseTenantry` replaces any Tenantry filter already added: calling
it again does nothing new, and the services given last are the ones its jobs use.

If `pro.AddHangfirePropagation()` is called but Hangfire's configuration never calls `UseTenantry(sp)`, the
application fails to start with an `InvalidOperationException` that names the missing call: its jobs would
otherwise run without their tenant. To check, Tenantry resolves Hangfire's configuration as the application
starts if nothing has yet, as in an application that only enqueues jobs, so Hangfire sets up its storage then (a
SQL Server storage prepares its schema) rather than at the first enqueue.

## Behaviour

The tenant's id is stored in the job's parameters, under `TenantPropagation.HeaderName`
(`tenantry-tenant-id`). The job runs as whatever tenant its storage names, so keep job storage writable only by your
application.

| Scenario | Result |
|----------|--------|
| Job enqueued while a tenant is current | The job runs as that tenant |
| Job enqueued with no tenant current | `OnMissingTenant` applies (default `Warn`: the job runs without a tenant, and a warning is logged) |
| The job's tenant is not in the store, or its id is not a valid id | `OnUnresolvedTenant` applies (default `Reject`: the job fails, and Hangfire's retries apply) |
| Tenant that `ValidateTenantActivity` refuses | As a tenant the store does not have: `OnUnresolvedTenant` applies ([Suspended tenants](background-jobs.md#suspended-tenants)) |
| Job enqueued through `jobs.WithTenant(id)` | The job runs as that tenant, whichever tenant is current ([Enqueueing a job for a tenant](#enqueueing-a-job-for-a-tenant)) |
| Recurring job (`RecurringJob.AddOrUpdate`) | Hangfire's scheduler creates each run outside any request, so it carries no tenant and `OnMissingTenant` applies to every run. For work per tenant, use `AddOrUpdateForEachTenant` ([Recurring jobs](#recurring-jobs)) |

While a job runs as its tenant, its logs carry a `TenantId` scope, and the job's trace span, if a tracing filter
(OpenTelemetry's Hangfire instrumentation, say) starts one, is tagged `tenant.id`
([Telemetry](telemetry.md#logs-and-traces)).

## Enqueueing a job for a tenant

To enqueue a job on a tenant's behalf from code that runs without one (an administrator's request, a system task),
or for another tenant than the current one, enqueue it through `WithTenant`:

```csharp
using Hangfire;

public sealed class ReportRequests(IBackgroundJobClient jobs)
{
    public string Enqueue(string tenantId) =>
        jobs.WithTenant(tenantId).Enqueue<ReportJob>(job => job.Execute());   // runs as tenantId
}
```

`WithTenant` returns a client like the one it wraps, so `Schedule` and `ContinueJobWith` work through it too. Each
job it creates carries the tenant in its parameters, and Tenantry's filter leaves it there rather than replace it
with the current tenant. The tenant is looked up when the job runs, so `OnUnresolvedTenant` applies to an id the
store does not have. `WithTenant` needs a client that takes job parameters (`IBackgroundJobClientV2`), as
Hangfire's own does, and refuses the id Tenantry reserves for "no tenant" (`Guid.Empty`, `0`, an empty string).

## Jobs without a tenant, or with one that cannot be found

Two settings decide what happens to a job whose tenant cannot be made current:

- `OnMissingTenant`: the job carries no tenant. Default `Warn`.
- `OnUnresolvedTenant`: the job carries a tenant id that the store does not have (a tenant deleted since it was
  enqueued), or that is not a valid id of the key type. Default `Reject`, so the job never runs as no tenant.

```csharp
using Tenantry.Pro;

pro.AddHangfirePropagation(o =>
{
    o.OnMissingTenant = TenantPropagationBehavior.Reject;
    o.OnUnresolvedTenant = TenantPropagationBehavior.Skip;
});
```

| `TenantPropagationBehavior` | Effect |
|-------------------------|--------|
| `Allow` | Run the job without a tenant, silently. |
| `Warn` | Run the job without a tenant, and log a warning. |
| `Skip` | Do not run the job, and log a warning: Hangfire deletes it ("Canceled by filter"). |
| `Reject` | Throw `TenantNotResolvedException` (`TenantNotFoundException` for a tenant the store does not have): Hangfire records the job as failed and applies its retry policy. |

Use `Allow` for jobs that legitimately run without a tenant (global maintenance), and `Reject` or `Skip` to make
a job enqueued without a tenant fail or drop rather than run without one. `Reject` and `Skip` also apply to
recurring jobs added with `RecurringJob.AddOrUpdate`, which never have a tenant; one added with
`AddOrUpdateForEachTenant` is not affected (see [Recurring jobs](#recurring-jobs)). The same settings exist on
every Tenantry.Pro integration (MassTransit, Quartz.NET, Rebus), each set separately.

## Accessing the tenant inside a job

Hangfire creates the job, and the scoped services it takes, once the tenant is current:

```csharp
using Tenantry;

public class ReportJob(ITenantContext<string> tenantContext)
{
    public void Execute()
    {
        // The tenant that was current when the job was enqueued
        var id = tenantContext.CurrentTenantId;
    }
}
```

## Recurring jobs

A recurring job added with `RecurringJob.AddOrUpdate` has no tenant: Hangfire's scheduler creates each run, outside
any request. To run work for every tenant on a schedule, add it with `AddOrUpdateForEachTenant` instead: each time
it is due, it enqueues the job once for each tenant in the store, and each of those jobs runs as its tenant.

```csharp
using Hangfire;

public sealed class ReportSchedule(IRecurringJobManager recurringJobs)
{
    public void Register() =>
        recurringJobs.AddOrUpdateForEachTenant<ReportJob>("nightly-reports", job => job.Execute(), Cron.Daily());
}
```

- The recurring job runs without a tenant, whatever `OnMissingTenant` is, and the dashboard shows it as
  `nightly-reports: enqueue for each tenant`. It reads the tenants from the store (`ITenantLookup<TKey>`);
  if the store cannot be read, it fails and Hangfire retries it. If it fails or is stopped part-way through
  enqueueing, Hangfire runs it again, and it enqueues every tenant's job again, so make the job safe to run twice.
- Each tenant's job is a job of its own, retried and shown on its own. They go to the default queue, or to the one
  a `[Queue]` attribute on the method names.
- It enqueues a job for every tenant the store returns that `ValidateTenantActivity` allows
  ([Suspended tenants](background-jobs.md#suspended-tenants)).
- The server that runs it needs the integration: `pro.AddHangfirePropagation()`, and `UseTenantry(sp)` on Hangfire's
  configuration. A server without them fails the recurring job with an error that says so.

## Limitations

- Hangfire's server filters are synchronous, so the tenant lookup blocks the worker thread. Workers are
  background threads, not request threads.
- Jobs enqueued with no tenant current, and not through `WithTenant`, carry none. Inject `ITenantContext<TKey>` and
  check `HasTenant` if a job must handle both cases.
