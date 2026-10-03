using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro.Samples.TenantHealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Integrated Security=true;TrustServerCertificate=true
var serverConnection = builder.Configuration.GetConnectionString("Server")
    ?? throw new InvalidOperationException("Connection string 'Server' is required.");

builder.Services
    .AddTenantry<string>(tenant =>
    {
        // The health checks enumerate every tenant from the store and probe each one: no request needs a tenant, so
        // the application registers no resolver and has no app.UseTenantry().
        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        tenant.UseConnectionStrings(opts =>
            opts.GetConnectionString = t => $"{serverConnection};Database=app_{t.TenantId}");

        tenant.UsePro();

        // The checks connect through the application's own context, in each tenant's scope.
        tenant.AddDbContextPerTenantDatabase<AppDbContext>((_, opts) => opts.UseSqlServer());
    });

builder.Services.AddHealthChecks()
    // Verify every tenant database is reachable, once per database.
    .AddTenantDatabaseCheck<AppDbContext>()
    // Verify every tenant database has AppDbContext's EF Core migrations applied.
    .AddTenantMigrationCheck<AppDbContext>();

var app = builder.Build();

// Liveness for your orchestrator: the process is running. Never include the tenant checks in a probe:
// one unreachable tenant database would fail it on every replica at once.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// Tenant monitoring: the aggregate status of every tenant database. The per-tenant data (tenant ids,
// provider error messages) is sensitive, so in production add .RequireAuthorization(...) and block
// /health/tenants at your ingress; a second, internal port alone does not restrict it. See docs/health-checks.md.
app.MapHealthChecks("/health/tenants", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("tenantry")
});

await app.RunAsync();
