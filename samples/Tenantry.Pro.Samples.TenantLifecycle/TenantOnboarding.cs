using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tenantry;
using Tenantry.Pro;

namespace Tenantry.Pro.Samples.TenantLifecycle;

/// <summary>
/// Onboards tenants: adds each to the catalog, provisions it (create its database, migrate, seed), and makes it
/// active once every step has succeeded. A tenant whose provisioning failed stays in the catalog, not active, until
/// provisioning it again succeeds.
/// </summary>
public sealed partial class TenantOnboarding(
    CatalogDbContext catalog,
    ITenantProvisioner<string> provisioner,
    ITenantInvalidator<string> invalidator)
{
    /// <summary>
    /// Whether an id may name a new tenant. It becomes part of the tenant's database name and connection string, so
    /// only lowercase letters, digits and hyphens are accepted.
    /// </summary>
    public static bool IsValidId([NotNullWhen(true)] string? tenantId) => tenantId is not null && ValidId().IsMatch(tenantId);

    /// <summary>Adds a tenant and provisions it; <see langword="null"/> if the catalog already has the id.</summary>
    public async Task<TenantProvisioningResult<string>?> OnboardAsync(string tenantId, string name, CancellationToken ct)
    {
        if (await catalog.Tenants.AnyAsync(t => t.TenantId == tenantId, ct))
            return null;

        // The provisioner needs the tenant in the store first. Until it is active, the access validator refuses it.
        var tenant = new CatalogTenant { TenantId = tenantId, Name = name, Status = TenantStatus.Provisioning };
        catalog.Tenants.Add(tenant);
        try
        {
            await catalog.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request added the same id since the check above.
            catalog.Entry(tenant).State = EntityState.Detached;
            if (await catalog.Tenants.AnyAsync(t => t.TenantId == tenantId, ct))
                return null;

            throw;
        }

        return await ProvisionAsync(tenant, ct);
    }

    /// <summary>Provisions a tenant in the catalog again; <see langword="null"/> if there is no such tenant.</summary>
    public async Task<TenantProvisioningResult<string>?> RetryAsync(string tenantId, CancellationToken ct) =>
        await catalog.Tenants.FindAsync([tenantId], ct) is { } tenant ? await ProvisionAsync(tenant, ct) : null;

    /// <summary>Suspends a tenant: its requests are refused from now on.</summary>
    public async Task<bool> SuspendAsync(string tenantId, CancellationToken ct)
    {
        if (await catalog.Tenants.FindAsync([tenantId], ct) is not { } tenant)
            return false;

        tenant.Status = TenantStatus.Suspended;
        await catalog.SaveChangesAsync(ct);

        // The cache would serve the tenant as active until its entry expired. The catalog's id, not the one asked for:
        // the database may have matched another casing.
        await invalidator.InvalidateAsync(tenant.TenantId, ct);
        return true;
    }

    private async Task<TenantProvisioningResult<string>> ProvisionAsync(CatalogTenant tenant, CancellationToken ct)
    {
        var result = await provisioner.ProvisionAsync(tenant, ct);

        // Activate only a tenant the catalog still has as provisioning, so a suspension made while the steps ran is
        // not overwritten.
        if (result.Succeeded && await catalog.Tenants
                .Where(t => t.TenantId == tenant.TenantId && t.Status == TenantStatus.Provisioning)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.Status, TenantStatus.Active), ct) == 1)
        {
            // A request made while the tenant was provisioning cached it as not active.
            await invalidator.InvalidateAsync(tenant.TenantId, ct);
        }

        return result;
    }

    [GeneratedRegex(@"^[a-z0-9-]{1,32}\z")]
    private static partial Regex ValidId();
}
