using Tenantry;
using Tenantry.Pro.Samples.BackgroundWorker;

var builder = Host.CreateApplicationBuilder(args);

// AddTenantry from Tenantry.Core: a worker host has no HTTP pipeline and no resolvers.
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.UseInMemoryStore(
    [
        new Tenant { TenantId = "acme", Name = "Acme" },
        new Tenant { TenantId = "globex", Name = "Globex" },
        new Tenant { TenantId = "initech", Name = "Initech", IsActive = false },
    ]);

    // Work runs only for active tenants: the sweep skips initech.
    tenant.ValidateTenantActivity(t => t.As<Tenant>().IsActive);

    // UsePro reads the licence key from the Tenantry:License setting.
    tenant.UsePro();
});

// How often the sweep runs: every 30 seconds unless Sweep:Interval says otherwise (--Sweep:Interval 00:00:05).
builder.Services.AddSingleton(new SweepSettings(builder.Configuration.GetValue("Sweep:Interval", TimeSpan.FromSeconds(30))));
builder.Services.AddScoped<GreetingService>();
builder.Services.AddHostedService<GreetingSweep>();

await builder.Build().RunAsync();
