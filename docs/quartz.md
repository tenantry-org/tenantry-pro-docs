# Quartz.NET Integration

A Quartz.NET job scheduled for a tenant runs as that tenant: the job, and the scoped services its constructor
takes (a `DbContext` that picks the tenant's connection, say), are created once the tenant is current, and see it
in `ITenantContext<TKey>`. Store the tenant in the job's data when you schedule it, with `WithTenant`, or mark a
recurring job to run for each tenant, with `ForEachTenant`. The
[QuartzScheduling sample](../samples/Tenantry.Pro.Samples.QuartzScheduling) does both.

## Requirements

- `Tenantry.Pro.Quartz` NuGet package
- Quartz 3.8 or later 3.x, with `Quartz.Extensions.DependencyInjection` (the Microsoft DI integration)

## Registration

```csharp
using Quartz;

builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro(pro => pro.AddQuartzPropagation()));

builder.Services.AddQuartz(q => q.UseTenantry());
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
```

`pro.AddQuartzPropagation()` registers the integration; `q.UseTenantry()` sets Tenantry's job factory, which makes
the job's tenant current and then creates the job with Quartz's own DI job factory. The two calls can come in
either order. `q.UseTenantry()` replaces the job factory, so set no other job factory after it.

If `pro.AddQuartzPropagation()` is called but Quartz's configuration never calls `q.UseTenantry()`, or a job
factory set after it replaces Tenantry's, the application fails to start with an `InvalidOperationException` that
names the missing call: its jobs would otherwise run without their tenant.

> **Why a job factory and not an `IJobListener`?** An `IJobListener` would set the ambient tenant in
> `JobToBeExecuted`, which Quartz awaits as a separate step before it awaits `IJob.Execute`. The `AsyncLocal` set
> in the listener is undone before the job runs, so the job never sees the tenant. Creating and running the job
> inside the tenant, from the job factory, is what makes it visible.

## Scheduling a job for a tenant

```csharp
var job = JobBuilder.Create<ReportJob>()
    .UsingJobData(new JobDataMap().WithTenant(tenantId))   // the tenant the job runs as
    .Build();

var trigger = TriggerBuilder.Create().StartNow().Build();
await scheduler.ScheduleJob(job, trigger);
```

`WithTenant` stores the id as a string, formatted with the invariant culture, under `TenantPropagation.HeaderName`
(`tenantry-tenant-id`). It can go in the job's data or in a trigger's, which wins for that trigger's runs.

A job scheduled while a tenant is current does not carry it unless you call `WithTenant`, unlike in the other
integrations: Quartz.NET has no point at which a job or trigger can be changed before the scheduler stores it (its
listeners run after), and a scheduler that wrapped yours would miss the one a running job is given. To schedule a
job for the current tenant, pass it:

```csharp
using Quartz;
using Tenantry;

public sealed class ReportRequests(ISchedulerFactory schedulers, ITenantContext<string> tenantContext)
{
    public async Task RequestAsync()
    {
        var scheduler = await schedulers.GetScheduler();
        await scheduler.ScheduleJob(
            JobBuilder.Create<ReportJob>().UsingJobData(new JobDataMap().WithTenant(tenantContext.CurrentTenantId!)).Build(),
            TriggerBuilder.Create().StartNow().Build());
    }
}
```

## A job for each tenant

To run work for every tenant on one schedule, mark the job's data with `ForEachTenant`:

```csharp
builder.Services.AddQuartz(q =>
{
    q.UseTenantry();
    q.AddJob<ReportJob>(job => job
        .WithIdentity("nightly-reports")
        .UsingJobData(new JobDataMap().ForEachTenant()));
    q.AddTrigger(trigger => trigger.ForJob("nightly-reports").WithCronSchedule("0 0 2 * * ?"));
});
```

Each time the job fires without a tenant, Tenantry reads every tenant from the store (`ITenantLookup<TKey>`)
and schedules a run of the job for each, starting now, with the data and priority of the trigger that fired and the
tenant's id. Each tenant's run executes, and can fail, on its own:

- The firing that schedules the runs does not create or run the job, whatever `OnMissingTenant` is, but Quartz
  counts it as an execution of the job: job listeners see it, as well as each tenant's run. If the store cannot be
  read, that firing fails, and the next one tries again.
- A run whose data also names a tenant (`WithTenant`) runs as that tenant only.
- It triggers a run for every tenant the store returns, so a job must check whether your application has
  [suspended its tenant](background-jobs.md#suspended-tenants).
- A job marked `[DisallowConcurrentExecution]` runs for one tenant at a time.

## Behaviour

| Scenario | Result |
|----------|--------|
| Job scheduled with `WithTenant(id)`, and the tenant is in the store | The job runs as that tenant |
| Job marked `ForEachTenant()`, firing without a tenant | The job is triggered once for each tenant, each run as its tenant ([A job for each tenant](#a-job-for-each-tenant)) |
| Job scheduled without a tenant | `OnMissingTenant` applies (default `Warn`: the job runs without a tenant, and a warning is logged) |
| The stored tenant is not in the store, or its id is not a valid id | `OnUnresolvedTenant` applies (default `Reject`: the run fails) |
| Tenant is in the store but suspended by your app | The job runs as that tenant: Tenantry does not check status, so [your job must check](background-jobs.md#suspended-tenants) |

While a job runs as its tenant, its logs carry a `TenantId` scope, and the trace span it runs in, if tracing
records one, is tagged `tenant.id` ([Telemetry](telemetry.md#logs-and-traces)).

## Jobs without a tenant, or with one that cannot be found

Two settings decide what happens to a job whose tenant cannot be made current:

- `OnMissingTenant`: the job's data has no tenant. Default `Warn`.
- `OnUnresolvedTenant`: the job's data has a tenant id that the store does not have (a tenant deleted since the job
  was scheduled), or that is not a valid id of the key type. Default `Reject`, so the job never runs as no tenant.

```csharp
using Tenantry.Pro;

pro.AddQuartzPropagation(o => o.OnMissingTenant = TenantPropagationBehavior.Skip);
```

| `TenantPropagationBehavior` | Effect |
|-------------------------|--------|
| `Allow` | Run the job without a tenant, silently. |
| `Warn` | Run the job without a tenant, and log a warning. |
| `Skip` | Do not create or run the job, and log a warning. Quartz counts the run as done. |
| `Reject` | Throw `TenantNotResolvedException` (`TenantNotFoundException` for a tenant the store does not have). Quartz logs it and tells job listeners, wrapped in a `JobExecutionException`; the job's triggers keep firing. |

The same settings exist on every Tenantry.Pro integration, each set separately.

## Accessing the tenant inside a job

```csharp
using Tenantry;

public sealed class ReportJob(ITenantContext<string> tenantContext) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        // The tenant the job was scheduled for
        var id = tenantContext.CurrentTenantId;
        return Task.CompletedTask;
    }
}
```

## Limitations

- Jobs scheduled without `WithTenant(...)` or `ForEachTenant()` (global maintenance, say) carry no tenant. Inject
  `ITenantContext<TKey>` and check `HasTenant` if a job must handle both cases.
- The tenant store is read once for each run of a job, before the job is created.
- Because the job is created when it runs, an exception from its constructor surfaces as the job's exception
  (Quartz logs it, wraps it in a `JobExecutionException` and tells job listeners), not as a failure to create the
  job. So Quartz does not put the job's triggers in the `Error` state: they keep firing, and each firing fails the
  same way until the job's dependencies are fixed.
- `IJobExecutionContext.JobInstance` is Tenantry's stand-in, not your job.
