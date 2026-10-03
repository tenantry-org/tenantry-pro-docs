using MassTransit;
using Tenantry;
using Tenantry.Pro.Samples.MassTransitMessaging;

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
            // A message carries the tenant it was published or sent for, and is consumed as that tenant. Set what
            // happens to a message without one here, e.g. Skip to move it to the _skipped queue:
            //   pro.AddMassTransitPropagation(o => o.OnMissingTenant = Tenantry.Pro.TenantPropagationBehavior.Skip)
            pro.AddMassTransitPropagation();
        });
    });

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();

    x.UsingInMemory((ctx, cfg) =>
    {
        // Adds Tenantry's publish, send and consume filters, for every receive endpoint.
        cfg.UseTenantry(ctx);
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseTenantry();

// Publishing while a tenant is current puts the tenant in a header; the consumer runs as that tenant.
app.MapPost("/orders", async (IPublishEndpoint bus) =>
{
    await bus.Publish(new OrderPlaced(Guid.NewGuid().ToString("N")));
    return Results.Accepted();
}).RequireTenant(); // 400 without X-Tenant-Id

await app.RunAsync();
