# Quartz.NET Integration

Use this package to propagate the current tenant context into Quartz.NET scheduled jobs. Stamp the
tenant ID into a job's `JobDataMap` when you schedule it; when the job executes, it runs inside the
matching tenant scope so any code inside the job sees the correct `ITenantContext<TKey>`.

## What Tenantry.Pro.Quartz Provides

- A job-factory decorator that wraps every produced job so it executes **inside** the tenant scope
  (the scope wraps `IJob.Execute`, so the ambient tenant flows down into the job).
- `pro.AddQuartzTenantScope()` — decorates the Quartz-registered `IJobFactory` in DI.
- `JobDataMap.WithTenant(tenantId)` — stamps the tenant ID into a job's data map at schedule time.
- `TenantJobData.TenantIdKey` — the well-known data-map key used to carry the tenant ID.

> **Why a job-factory decorator and not an `IJobListener`?** An `IJobListener` sets the ambient
> tenant in `JobToBeExecuted`, which Quartz awaits as a *separate* step before it awaits
> `IJob.Execute`. The `AsyncLocal` set in the listener is unwound before the job runs, so the job
> never sees the tenant. Wrapping `Execute` (via the job factory) is the only mechanism that makes
> the ambient scope visible to the job.

## Requirements

- `Tenantry.Pro.Quartz` NuGet package
- Quartz 3.8+ and `Quartz.Extensions.DependencyInjection` (Microsoft DI integration)

## Registration

```csharp
using Tenantry.Pro;
using Tenantry.Pro.Quartz.Extensions;

// 1. Configure Quartz with the Microsoft DI integration FIRST so an IJobFactory is registered.
builder.Services.AddQuartz(q =>
{
    // register your jobs / triggers here
});
builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

// 2. Register Tenantry and decorate the Quartz job factory.
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddQuartzTenantScope();   // decorates the IJobFactory registered by AddQuartz above
    });
});
```

> **Ordering matters:** call `pro.AddQuartzTenantScope()` *after* `services.AddQuartz(...)`. The
> decorator wraps the `IJobFactory` that `AddQuartz` registers; if Quartz has not been added yet,
> `AddQuartzTenantScope()` throws `InvalidOperationException`.

## Scheduling a job for a tenant

```csharp
var job = JobBuilder.Create<ReportJob>()
    .UsingJobData(new JobDataMap().WithTenant(tenantId))   // stamp the tenant
    .Build();

var trigger = TriggerBuilder.Create().StartNow().Build();
await scheduler.ScheduleJob(job, trigger);
```

## Behaviour

| Scenario | Result |
|----------|--------|
| Job scheduled with `WithTenant(id)` and the tenant is in the store | Job runs inside that tenant's scope |
| Job scheduled without a tenant stamp | Missing-tenant policy applies (default: warn, run without scope) |
| Stamped tenant ID is unparseable as `TKey` | Missing-tenant policy applies (default: warn, run without scope) |
| Stamped tenant ID not found in the store | Missing-tenant policy applies (default: warn, run without scope) |

## Missing-tenant policy

When a job has no resolvable tenant, `AddQuartzTenantScope` decides what to do via
`TenantPropagationOptions.OnMissingTenant`:

```csharp
pro.AddQuartzTenantScope(o => o.OnMissingTenant = MissingTenantBehavior.Skip);
```

`Allow` runs the job without a scope silently; `Warn` (default) does the same and logs; `Reject`
throws (Quartz surfaces the job exception); `Skip` does not run the job. The same option exists on
every Tenantry.Pro integration and mirrors core's `EfCoreIsolationOptions.OnMissingTenant`.

## Accessing the tenant inside a job

```csharp
public sealed class ReportJob(ITenantContext<string> tenantContext) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // tenantContext.CurrentTenantId is the tenant stamped at schedule time
        var id = tenantContext.CurrentTenantId;
        return Task.CompletedTask;
    }
}
```

## Limitations

- Jobs scheduled without `WithTenant(...)` (e.g. global maintenance jobs) execute without a scope.
  Inject `ITenantContext<TKey>` and check `HasTenant` if a job must handle both cases.
- The tenant store is consulted once per job execution (asynchronously, before the job runs) to load
  the tenant descriptor for the stamped ID.
