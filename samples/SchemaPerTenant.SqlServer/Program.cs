using Microsoft.EntityFrameworkCore;
using SchemaPerTenant.SqlServer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.EfCore;
using Tenantry.Pro.EfCore.Extensions;
using Tenantry.Pro.EfCore.SqlServer.Extensions;
using Tenantry.Pro.EfCore.SqlServer.Strategies.SchemaPerTenant;

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

        // Schema-per-tenant: one shared database, one SQL Server schema per tenant.
        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            // Schema name == tenant id, matching AppDbContext.Schema.
            pro.UseSchemaPerTenant(opts =>
                opts.GetSchemaName = t => t.TenantId);

            // One compiled EF Core model per tenant schema (pairs with AddSchemaPerTenantCaching(sp) below).
            pro.AddSchemaPerTenantCaching();

            // On-demand schema provisioning (idempotent CREATE SCHEMA). The shared-database connection
            // string lives on the provisioning options.
            pro.AddSchemaProvisioning(opts =>
                opts.ConnectionString = connectionString);
        });
    });

// Register AppDbContext with per-tenant schema caching from Tenantry.Pro.EfCore.
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
    opts.UseSqlServer(connectionString)
        .AddSchemaPerTenantCaching<string>(sp));

var app = builder.Build();

app.UseTenantry();

// Onboard the calling tenant: create its schema, then its tables the first time. Migration orchestration
// covers database per tenant only, so a schema tenant's tables are created here; later schema changes need
// your own per-schema scripts (see the schema-per-tenant guide).
app.MapPost("/provision",
    async (SchemaProvisioningService<string> provisioning, ITenantContext<string> ctx, AppDbContext db, CancellationToken ct) =>
    {
        if (!ctx.HasTenant) return Results.BadRequest("No tenant resolved (set the X-Tenant-Id header).");

        var schema = ctx.CurrentTenantId!;
        await provisioning.ProvisionAsync(schema, ct);   // CREATE SCHEMA, idempotent

        var tables = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM information_schema.tables WHERE table_schema = {schema}")
            .SingleAsync(ct);

        if (tables == 0)
            await db.GetService<IRelationalDatabaseCreator>().CreateTablesAsync(ct);

        return Results.Ok($"Schema '{schema}' provisioned for tenant '{schema}'.");
    });

app.MapGet("/products", async (AppDbContext db) =>
    await db.Products.ToListAsync());

await app.RunAsync();
