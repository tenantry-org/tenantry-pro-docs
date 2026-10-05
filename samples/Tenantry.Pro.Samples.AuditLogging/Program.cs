using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro.EfCore;
using Tenantry.Pro.Samples.AuditLogging;

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
            // Record the changes (insert/update/delete) every context that uses UseTenantry() saves, with the
            // current tenant id, once they are committed.
            pro.AddAuditLogging();
        });
    });

// Replace the default logging audit store with one that keeps entries in memory for the demo.
builder.Services.AddSingleton<InMemoryAuditStore>();
builder.Services.AddSingleton<IAuditStore>(sp => sp.GetRequiredService<InMemoryAuditStore>());

// UseTenantry() isolates the context's tenant-owned entities, and with AddAuditLogging audits its saves. Uses the
// EF Core in-memory provider so the sample runs with no external database.
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseInMemoryDatabase("audit-sample")
        .UseTenantry());

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
