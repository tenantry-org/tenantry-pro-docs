using BackgroundWorker;
using Tenantry.Core;
using Tenantry.Core.Extensions;
using Tenantry.Pro;

var builder = Host.CreateApplicationBuilder(args);

// AddTenantryCore (not AddTenantry) — there is no HTTP pipeline and no resolvers in a worker host.
builder.Services.AddTenantryCore<string>(tenant =>
{
    tenant.UseInMemoryStore(
    [
        new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
        new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
    ]);

    // AddTenantryCore registers Core's ITenantScopeFactory<string> and ITenantStoreAccessor<string>.
    tenant.UsePro(pro =>
        pro.WithLicence(builder.Configuration["Tenantry:Licence"] ?? string.Empty));
});

builder.Services.AddScoped<GreetingService>();
builder.Services.AddHostedService<TenantSweepWorker>();

await builder.Build().RunAsync();
