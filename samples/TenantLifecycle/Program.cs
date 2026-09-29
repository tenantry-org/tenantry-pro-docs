using Microsoft.EntityFrameworkCore;
using TenantLifecycle;
using Tenantry.AspNetCore.Extensions;
using Tenantry.Core;
using Tenantry.Pro;
using Tenantry.Pro.EfCore;
using Tenantry.Pro.EfCore.SqlServer.Extensions;
using Tenantry.Pro.Lifecycle;
using Tenantry.Pro.Lifecycle.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Integrated Security=true;TrustServerCertificate=true
var serverConnection = builder.Configuration.GetConnectionString("Server")
    ?? throw new InvalidOperationException("Connection string 'Server' is required.");

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        // Tenants must exist in the store before they are provisioned (the pipeline looks them up).
        tenant.UseInMemoryStore(
        [
            new TenantDescriptor<string> { TenantId = "acme", Name = "Acme" },
            new TenantDescriptor<string> { TenantId = "globex", Name = "Globex" },
        ]);

        tenant.UsePro(pro =>
        {
            pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);

            pro.UseDatabasePerTenant(opts =>
                opts.GetConnectionString = t => $"{serverConnection};Database=app_{t.TenantId}");

            // The three pipeline steps. Each runs only because its service is registered:
            pro.AddDatabaseProvisioning();                          // 1. CREATE DATABASE
            pro.WithMigrationOrchestration<string, AppDbContext>(   // 2. apply EF Core migrations
                cs => new AppDbContext(
                    new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(cs).Options));

            pro.AddLifecycleManagement();                          // orchestrates provision -> migrate -> seed
        });
    });

// 3. The seeding step: a registered ITenantSeeder<string>.
builder.Services.AddScoped<ITenantSeeder<string>, DefaultSettingsSeeder>();
builder.Services.AddSingleton<SeedFaults>(); // demo only: simulate a failed seed

builder.Services.AddDbContext<AppDbContext>((sp, opts) =>
    opts.UseSqlServer(sp.GetRequiredService<ITenantConnectionStringResolver<string>>().Resolve()));

var app = builder.Build();

app.UseTenantry();

// Onboard a tenant: run provision -> migrate -> seed in one call and report how far it got.
// ?simulateSeedFailure=true makes seeding fail part-way through. Onboarding the tenant again is the
// recovery: every step is idempotent, so the retry completes without duplicating anything.
app.MapPost("/tenants/{id}/onboard",
    async (string id, bool? simulateSeedFailure, ITenantLifecycleManager<string> lifecycle,
        ITenantStore<string> store, SeedFaults faults, CancellationToken ct) =>
    {
        var descriptor = await store.GetTenantAsync(id, ct);
        if (descriptor is null) return Results.NotFound($"Tenant '{id}' is not registered.");

        if (simulateSeedFailure == true) faults.FailNextSeed(id);

        var result = await lifecycle.ProvisionAsync(descriptor, ct);

        return Results.Ok(new
        {
            result.TenantId,
            result.Succeeded,
            CompletedUpTo = result.CompletedUpTo.ToString(),
            DurationMs = result.Duration.TotalMilliseconds,
            Error = result.Error?.Message
        });
    });

// What the seeder wrote: after a failure and a retry, each setting appears exactly once.
app.MapGet("/tenants/{id}/settings",
    async (string id, ITenantStore<string> store, ITenantScopeFactory<string> scopes, CancellationToken ct) =>
    {
        var descriptor = await store.GetTenantAsync(id, ct);
        if (descriptor is null) return Results.NotFound($"Tenant '{id}' is not registered.");

        await using var scope = scopes.CreateScope(descriptor);
        var settings = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Settings
            .OrderBy(setting => setting.Id)
            .Select(setting => new { setting.Key, setting.Value })
            .ToListAsync(ct);

        return Results.Ok(settings);
    });

await app.RunAsync();
