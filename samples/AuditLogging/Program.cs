using AuditLogging;
using Microsoft.EntityFrameworkCore;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.EfCore.Audit;
using Tenantry.Pro.EfCore.Audit.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            // Record entity changes (insert/update/delete) with the current tenant id.
            pro.AddAuditLogging();
        });
    });

// Replace the default logging audit store with one that keeps entries in memory for the demo.
builder.Services.AddSingleton<InMemoryAuditStore>();
builder.Services.AddSingleton<IAuditStore>(sp => sp.GetRequiredService<InMemoryAuditStore>());

// Wire the audit interceptor into the DbContext. Uses the EF Core in-memory provider so the sample
// runs with no external database.
builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
    opts.UseInMemoryDatabase("audit-sample")
        .UseAuditLogging<string>(sp));

var app = builder.Build();

app.UseTenantry();

app.MapPost("/products", async (Product product, AppDbContext db) =>
{
    db.Products.Add(product);
    await db.SaveChangesAsync();   // produces an audit entry tagged with the current tenant
    return Results.Created($"/products/{product.Id}", product);
});

// Inspect everything the audit store captured.
app.MapGet("/audit", (InMemoryAuditStore store) =>
    store.Entries.Select(e => new
    {
        e.TableName,
        Action = e.Action.ToString(),
        e.PrimaryKey,
        e.TenantId,
        e.Timestamp,
        Changed = e.ChangedProperties
    }));

await app.RunAsync();
