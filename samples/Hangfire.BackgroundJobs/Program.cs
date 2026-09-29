using Hangfire;
using Hangfire.BackgroundJobs;
using Hangfire.SqlServer;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Pro;
using Tenantry.Pro.Hangfire.Extensions;

var builder = WebApplication.CreateBuilder(args);

var hangfireCs = builder.Configuration.GetConnectionString("Hangfire")
    ?? throw new InvalidOperationException("Connection string 'Hangfire' is required.");

builder.Services.AddHangfire(cfg =>
    cfg.UseSqlServerStorage(hangfireCs, new SqlServerStorageOptions()));

builder.Services.AddHangfireServer();

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            // Registers TenantJobFilter<string> in DI and stores IHangfireConfigurator
            // so the non-generic app.UseTenantryHangfire() call below can resolve TKey.
            pro.AddHangfireTenantFilter();
        });
    });

var app = builder.Build();

// Wire up the tenant job filter. TKey (string) is resolved from DI automatically.
app.UseTenantryHangfire();

app.UseHangfireDashboard();

app.MapPost("/jobs/report", (IBackgroundJobClient jobs) =>
{
    // The current request's tenant id is captured by the filter and replayed
    // when the job executes on a background thread.
    jobs.Enqueue<ReportJob>(j => j.GenerateAsync());
    return Results.Accepted();
});

await app.RunAsync();
