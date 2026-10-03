using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using Tenantry;
using Tenantry.Pro.Samples.RebusMessaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseInMemoryStore(
    [
        new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
        new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
    ]);

    tenant.UsePro(pro =>
    {
        // A message carries the tenant it was sent or published for, and is handled as that tenant.
        pro.AddRebusPropagation();
    });
});

// The in-memory transport, so the sample runs with no broker. The bus sends both messages to its own queue.
builder.Services.AddRebus((configure, sp) => configure
    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "orders"))
    .Routing(r => r.TypeBased().Map<OrderPlaced>("orders").Map<PaymentReminder>("orders"))
    .Options(o => o.UseTenantry(sp)));    // adds Tenantry's outgoing and incoming steps

builder.Services.AddRebusHandler<OrderPlacedHandler>();
builder.Services.AddRebusHandler<PaymentReminderHandler>();

var app = builder.Build();

app.UseTenantry();

// Sending while a tenant is current puts the tenant in a header; the handler runs as that tenant.
app.MapPost("/orders", async (IBus bus) =>
{
    await bus.Send(new OrderPlaced(Guid.NewGuid()));
    return Results.Accepted();
}).RequireTenant();

// Code that runs without a tenant (an administrator's request, a system task) names the tenant in the headers.
app.MapPost("/admin/tenants/{id}/payment-reminder", async (string id, IBus bus, ITenantLookup<string> tenants, CancellationToken ct) =>
{
    // The handler looks the tenant up again, and rejects a message for one the store does not have.
    if (await tenants.GetTenantAsync(id, ct) is null)
        return Results.NotFound();

    await bus.Send(new PaymentReminder(), new Dictionary<string, string>().WithTenant(id));
    return Results.Accepted();
});

await app.RunAsync();
