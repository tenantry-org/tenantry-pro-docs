using DatabasePerTenant.MySql;
using Microsoft.EntityFrameworkCore;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.EfCore;
using Tenantry.Pro.EfCore.Migrations;
using Tenantry.Pro.EfCore.MySql.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Port=3306;User Id=myuser;Password=mypassword
var serverConnection = builder.Configuration.GetConnectionString("Server")
    ?? throw new InvalidOperationException("Connection string 'Server' is required.");

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        // Use a database-backed store in production; the migration runner below migrates every tenant in it.
        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            // Database-per-tenant: a separate MySQL database for every tenant.
            // MySQL treats schemas and databases as synonyms; only this strategy is supported.
            pro.UseDatabasePerTenant(opts =>
                opts.GetConnectionString = t =>
                    $"{serverConnection};Database=myapp_{t.TenantId}");

            // On-demand database provisioning. Inject DatabaseProvisioningService<string>
            // in the endpoint that creates a new tenant and call ProvisionAsync(tenantId).
            pro.AddDatabaseProvisioning();

            // Tenant database migrations. They run as a separate deployment step (dotnet run -- migrate, below),
            // not at startup: with several instances, every one would otherwise migrate every tenant at once.
            pro.WithMigrationOrchestration<string, AppDbContext>(
                cs => new AppDbContext(
                    new DbContextOptionsBuilder<AppDbContext>()
                        .UseMySQL(cs)
                        .Options),
                runAtStartup: false);
        });
    });

// Register AppDbContext — connection string is resolved per-request via DI.
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
{
    var resolver = sp.GetRequiredService<ITenantConnectionStringResolver<string>>();
    var cs       = resolver.Resolve();
    opts.UseMySQL(cs);
});

var app = builder.Build();

app.UseTenantry();

app.MapGet("/orders", async (AppDbContext db) =>
    await db.Orders.ToListAsync())
    .RequireTenant(); // 400 without X-Tenant-Id, rather than failing to resolve a connection string

// The migration runner: run once per deployment (a CI/CD step, a Kubernetes Job or an init container) before
// starting the new version, rather than on every instance at startup.
//   dotnet run -- migrate
if (args.Contains("migrate"))
{
    var report = await app.Services.GetRequiredService<MigrationOrchestratorService<string, AppDbContext>>()
        .MigrateAllAsync();

    Console.WriteLine($"Migrated {report.Succeeded}/{report.Total} tenant database(s); {report.Failed} failed.");
    return report.HasFailures ? 1 : 0;
}

await app.RunAsync();
return 0;
