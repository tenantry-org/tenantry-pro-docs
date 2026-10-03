using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro.Samples.DatabasePerTenantSqlServer;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Integrated Security=true;TrustServerCertificate=true
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

        // Database per tenant: a separate SQL Server database for every tenant.
        tenant.UseConnectionStrings(opts =>
            opts.GetConnectionString = t => $"{serverConnection};Database=myapp_{t.TenantId}");

        tenant.UsePro(pro => pro
            // Tenant provisioning creates a new tenant's database through AppDbContext, then migrates it. Inject
            // ITenantProvisioner<string> where you create tenants and call ProvisionAsync(tenant).
            .AddDatabaseProvisioning<AppDbContext>()
            // Tenant database migrations. They run as a separate deployment step (dotnet run -- migrate-tenants,
            // below), not at startup: with several instances, every one would otherwise migrate every tenant at once.
            .AddMigrations<AppDbContext>());

        // Each AppDbContext connects to the current tenant's database.
        tenant.AddDbContextPerTenantDatabase<AppDbContext>((_, opts) => opts.UseSqlServer());
    });

await using var app = builder.Build();   // disposing it at exit writes out the last log messages

// The migration runner: run once per deployment, from one process (a CI/CD step or a Kubernetes Job, not an init
// container, which runs in every replica), before starting the new version. It migrates every tenant and exits.
//   dotnet run -- migrate-tenants
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;

app.UseTenantry();

app.MapGet("/orders", async (AppDbContext db) =>
    await db.Orders.ToListAsync())
    .RequireTenant(); // 400 without X-Tenant-Id, rather than failing to resolve a connection string

await app.RunAsync();
return 0;
