using MassTransit;
using MassTransit.Messaging;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.MassTransit.Extensions;

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

            // Registers the publish/send/consume tenant filters in DI. TKey is inferred from `pro`.
            // Configure the missing-tenant policy here, e.g. Skip to drop messages with no tenant:
            //   pro.AddMassTransitTenantFilters(o => o.OnMissingTenant = MissingTenantBehavior.Skip)
            pro.AddMassTransitTenantFilters();
        });
    });

builder.Services.AddMassTransit(x =>
{
    // Restore the tenant scope on every receive endpoint (TKey resolved from DI).
    x.AddTenantryConsumeFilter();

    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingInMemory((ctx, cfg) =>
    {
        // Stamp the tenant id onto outgoing messages (publish + send).
        cfg.UseTenantryPro(ctx);
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseTenantry();

// Publishing inside an active tenant scope stamps the tenant header; the consumer restores it.
app.MapPost("/orders", async (IPublishEndpoint bus, ITenantContext<string> ctx) =>
{
    if (!ctx.HasTenant) return Results.BadRequest("No tenant resolved (set the X-Tenant-Id header).");
    await bus.Publish(new OrderPlaced(Guid.NewGuid().ToString("N")));
    return Results.Accepted();
});

await app.RunAsync();
