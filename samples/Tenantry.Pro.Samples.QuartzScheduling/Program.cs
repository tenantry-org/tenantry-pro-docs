using Quartz;
using Tenantry;
using Tenantry.Pro.Samples.QuartzScheduling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseInMemoryStore(
    [
        new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
        new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
    ]);

    tenant.UsePro(pro =>
    {
        // A job whose data names a tenant runs as that tenant.
        pro.AddQuartzPropagation();
    });
});

// Quartz's in-memory job store, so the sample runs without a database. UseTenantry() sets Tenantry's job factory.
builder.Services.AddQuartz(q =>
{
    q.UseTenantry();

    // Recurring work for every tenant: each firing schedules one run of the job for each tenant in the store.
    q.AddJob<CleanupJob>(job => job
        .WithIdentity("cleanup")
        .UsingJobData(new JobDataMap().ForEachTenant()));
    q.AddTrigger(trigger => trigger
        .ForJob("cleanup")
        .StartNow()
        .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(1)).RepeatForever()));
});
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

// A scoped service the jobs depend on: it sees the job's tenant, as it would in a request.
builder.Services.AddScoped<ReportWriter>();

var app = builder.Build();

app.UseTenantry();

// A one-off job for the request's tenant. Quartz stores a job without the tenant that was current when it was
// scheduled, so pass the tenant in the job's data.
app.MapPost("/jobs/report", async (ISchedulerFactory schedulers, ITenantContext<string> tenantContext, CancellationToken ct) =>
{
    var scheduler = await schedulers.GetScheduler(ct);
    await scheduler.ScheduleJob(
        JobBuilder.Create<ReportJob>().UsingJobData(new JobDataMap().WithTenant(tenantContext.CurrentTenantId!)).Build(),
        TriggerBuilder.Create().StartNow().Build(),
        ct);

    return Results.Accepted();
}).RequireTenant();

await app.RunAsync();
