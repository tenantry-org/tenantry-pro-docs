using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;
using Tenantry.Pro.Samples.TenantLifecycle;

var builder = WebApplication.CreateBuilder(args);

// Template: Server=localhost;Integrated Security=true;TrustServerCertificate=true
var serverConnection = builder.Configuration.GetConnectionString("Server")
    ?? throw new InvalidOperationException("Connection string 'Server' is required.");

// The catalog database, which lists the tenants. Each tenant's own data is in its own database, app_<id>.
builder.Services.AddDbContext<CatalogDbContext>(options => options.UseSqlServer($"{serverConnection};Database=catalog"));

builder.Services
    .AddTenantry<string>(tenant =>
    {
        tenant.ResolveFromHeader("X-Tenant-Id");

        // Tenants come from the catalog, cached so that a request does not query it each time.
        tenant.UseStore<CatalogTenantStore>();
        tenant.CacheTenants();

        // Requests may use active tenants only: one still provisioning, or suspended, gets 403.
        tenant.ValidateTenantAccess((_, t) => t is CatalogTenant { Status: TenantStatus.Active });

        tenant.UseConnectionStrings(opts =>
            opts.GetConnectionString = t => $"{serverConnection};Database=app_{t.TenantId}");

        // The provisioning steps, which run in this order whatever the order of these calls:
        tenant.UsePro(pro => pro
            .AddSeeder<DefaultSettingsSeeder>()          // 3. seed data
            .AddDatabaseProvisioning<AppDbContext>()     // 1. CREATE DATABASE
            .AddMigrations<AppDbContext>());             // 2. apply EF Core migrations

        tenant.AddDbContextPerTenantDatabase<AppDbContext>((_, opts) => opts.UseSqlServer());
    });

builder.Services.AddScoped<TenantOnboarding>();
builder.Services.AddSingleton<SeedFaults>(); // demo only: simulate a failed seed

var app = builder.Build();

// Create the catalog database, or bring it up to date, before serving requests.
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
}

app.UseTenantry();

// Onboard a tenant: add it to the catalog, then create its database, migrate it and seed it, and report each step.
// ?simulateSeedFailure=true makes seeding fail part-way through.
app.MapPost("/tenants", async (NewTenant request, bool? simulateSeedFailure, TenantOnboarding onboarding,
    SeedFaults faults, CancellationToken ct) =>
{
    if (!TenantOnboarding.IsValidId(request.TenantId) || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 256)
        return Results.BadRequest("A tenant needs a name of up to 256 characters, and an id of up to 32 lowercase letters, digits and hyphens.");

    if (simulateSeedFailure == true)
        faults.FailNextSeed(request.TenantId);

    return await onboarding.OnboardAsync(request.TenantId, request.Name, ct) is { } result
        ? Results.Ok(Report(result))
        : Results.Conflict($"Tenant '{request.TenantId}' already exists.");
});

// Provision a tenant again, after a failure. Every step is idempotent, so the retry completes without duplicating
// anything, and the tenant becomes active.
app.MapPost("/tenants/{id}/provision", async (string id, TenantOnboarding onboarding, CancellationToken ct) =>
    await onboarding.RetryAsync(id, ct) is { } result
        ? Results.Ok(Report(result))
        : Results.NotFound($"Tenant '{id}' is not in the catalog."));

app.MapPost("/tenants/{id}/suspend", async (string id, TenantOnboarding onboarding, CancellationToken ct) =>
    await onboarding.SuspendAsync(id, ct) ? Results.NoContent() : Results.NotFound($"Tenant '{id}' is not in the catalog."));

// A tenant's own request (X-Tenant-Id): what the seeder wrote, in the tenant's database.
app.MapGet("/settings", async (AppDbContext db, CancellationToken ct) =>
    await db.Settings
        .OrderBy(setting => setting.Id)
        .Select(setting => new { setting.Key, setting.Value })
        .ToListAsync(ct))
    .RequireTenant();

await app.RunAsync();

static object Report(TenantProvisioningResult<string> result) => new
{
    result.TenantId,
    result.Succeeded,
    Steps = result.Steps.Select(step => new { step.Name, Status = step.Status.ToString(), Error = step.Error?.Message }),
    DurationMs = result.Duration.TotalMilliseconds,
};

internal sealed record NewTenant(string? TenantId, string? Name);
