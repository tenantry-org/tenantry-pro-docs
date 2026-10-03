using Tenantry;

namespace Tenantry.Pro.Samples.BackgroundWorker;

/// <summary>
/// A scoped, tenant-aware service. It depends on <see cref="ITenantContext{TKey}"/>, which is set by
/// the tenant scope the sweep opens, so it always sees the right tenant without being passed one.
/// In a real app this would be your DbContext, a repository, or domain services.
/// </summary>
public sealed class GreetingService(ITenantContext<string> tenantContext)
{
    public string Greeting() =>
        tenantContext.HasTenant
            ? $"Hello from tenant '{tenantContext.CurrentTenantId}' ({tenantContext.CurrentTenant!.Name})"
            : "Hello from no tenant";
}

/// <summary>
/// The application's own tenant type: Tenantry keeps no status, so a suspended tenant is one the application marks.
/// </summary>
public sealed class Tenant : TenantDescriptor<string>
{
    public bool IsActive { get; init; } = true;
}
