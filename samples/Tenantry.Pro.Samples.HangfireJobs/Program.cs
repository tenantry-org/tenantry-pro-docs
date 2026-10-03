using Hangfire;
using Tenantry;
using Tenantry.Pro.Samples.HangfireJobs;

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
        // A job carries the tenant it was enqueued for, and runs as that tenant.
        pro.AddHangfirePropagation();
    });
});

// In-memory job storage, so the sample runs without a database. Use durable storage in production, such as
// Hangfire.SqlServer: config.UseSqlServerStorage(connectionString). UseTenantry(sp) adds Tenantry's job filter.
builder.Services.AddHangfire((sp, config) => config.UseInMemoryStorage().UseTenantry(sp));
builder.Services.AddHangfireServer();

// A scoped service the job depends on: it sees the job's tenant, as it would in a request.
builder.Services.AddScoped<ReportWriter>();

var app = builder.Build();

app.UseTenantry();          // resolves the request's tenant, so enqueuing captures it
app.UseHangfireDashboard();

app.MapPost("/jobs/report", (IBackgroundJobClient jobs) =>
{
    jobs.Enqueue<ReportJob>(job => job.GenerateAsync());
    return Results.Accepted();
}).RequireTenant();

await app.RunAsync();
