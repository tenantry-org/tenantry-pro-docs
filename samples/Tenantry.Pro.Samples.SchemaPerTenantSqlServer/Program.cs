using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;
using Tenantry.Pro.Samples.SchemaPerTenantSqlServer;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is required.");

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        // Demo tenants. A real app backs the store with a database.
        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        // Schema per tenant: one shared SQL Server database, one schema per tenant.
        tenant.UsePro(pro => pro
            // Every context that uses UseTenantry() gets the current tenant's schema: here, its id.
            .UseSchemaPerTenant(opts => opts.GetSchemaName = t => t.TenantId)
            // Tenant provisioning: create the tenant's schema (CreateSchema), then apply the migrations in Migrations/
            // to it (Migrations), with a migration history of its own. To migrate every tenant's schema after a
            // change, run the runner as a deployment step (below).
            .AddSchemaProvisioning<AppDbContext>()
            .AddMigrations<AppDbContext>());
    });

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connectionString).UseTenantry());

await using var app = builder.Build();   // disposing it at exit writes out the last log messages

// Migrate every tenant's schema, as a deployment step, then exit: dotnet run -- migrate-tenants
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;

app.UseTenantry();

// Onboard the calling tenant: create its schema and migrate it.
app.MapPost("/provision", async (ITenantProvisioner<string> provisioner, ITenantContext<string> ctx, CancellationToken ct) =>
{
    var tenant = ctx.CurrentTenant!;
    var result = await provisioner.ProvisionAsync(tenant, ct);

    return result.Succeeded
        ? Results.Ok($"Schema '{tenant.TenantId}' provisioned for tenant '{tenant.TenantId}'.")
        : Results.Problem(result.Error?.Message);
}).RequireTenant(); // 400 without X-Tenant-Id

app.MapGet("/products", async (AppDbContext db) => await db.Products.ToListAsync())
    .RequireTenant(); // 400 without X-Tenant-Id

await app.RunAsync();
return 0;
