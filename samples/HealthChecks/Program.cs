using HealthChecks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.HealthChecks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Integrated Security=true;TrustServerCertificate=true
var serverConnection = builder.Configuration.GetConnectionString("Server")
    ?? throw new InvalidOperationException("Connection string 'Server' is required.");

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        // The health checks enumerate every tenant from the store and probe each one.
        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            pro.UseDatabasePerTenant(opts =>
                opts.GetConnectionString = t => $"{serverConnection};Database=app_{t.TenantId}");
        });
    });

builder.Services.AddHealthChecks()
    // Verify every tenant database is reachable.
    .AddTenantryDatabaseCheck<string>(opts =>
        opts.ConnectionFactory = cs => new SqlConnection(cs))
    // Verify every tenant database has its EF Core migrations applied.
    .AddTenantryMigrationCheck<string, AppDbContext>(
        cs => new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options));

var app = builder.Build();

// GET /health returns the aggregate status; per-tenant detail is in the health check data dictionary.
app.MapHealthChecks("/health");

await app.RunAsync();
