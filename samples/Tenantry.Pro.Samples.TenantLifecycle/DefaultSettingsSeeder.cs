using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;

namespace Tenantry.Pro.Samples.TenantLifecycle;

/// <summary>
/// Seeds initial data for a new tenant. Runs as the last provisioning step, resolved from a scope for the new
/// tenant, so the DbContext it takes connects to the new tenant's database.
/// </summary>
/// <remarks>
/// Failed provisioning is retried by calling <c>ProvisionAsync</c> again, which runs the seeder again. So the
/// seeder is idempotent: it adds each setting only if it is missing, and the unique index on
/// <see cref="Setting.Key"/> backs that up. A retry after a failure part-way through completes the seed
/// without duplicating what the first attempt wrote.
/// </remarks>
public sealed class DefaultSettingsSeeder(AppDbContext db, SeedFaults faults) : ITenantSeeder<string>
{
    public async Task SeedAsync(ITenantDescriptor<string> tenant, CancellationToken cancellationToken)
    {
        await EnsureSettingAsync(db, "DisplayName", tenant.Name, cancellationToken);

        // Demo only: fail part-way through, after the first setting is saved.
        if (faults.TakeSeedFailure(tenant.TenantId))
        {
            throw new InvalidOperationException(
                $"Simulated failure while seeding tenant '{tenant.TenantId}' (DisplayName was already saved).");
        }

        await EnsureSettingAsync(db, "Plan", "trial", cancellationToken);
    }

    private static async Task EnsureSettingAsync(AppDbContext db, string key, string value, CancellationToken ct)
    {
        if (await db.Settings.AnyAsync(setting => setting.Key == key, ct))
        {
            return;
        }

        db.Settings.Add(new Setting { Key = key, Value = value });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>Demo only: lets the onboarding endpoint make the next seed of a tenant fail.</summary>
public sealed class SeedFaults
{
    private readonly ConcurrentDictionary<string, bool> _pending = new();

    public void FailNextSeed(string tenantId) => _pending[tenantId] = true;

    public bool TakeSeedFailure(string tenantId) => _pending.TryRemove(tenantId, out _);
}
