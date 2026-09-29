using Tenantry.Core;

namespace BackgroundWorker;

/// <summary>
/// A scoped, tenant-aware service. It depends on <see cref="ITenantContext{TKey}"/>, which is set by
/// the tenant scope the worker opens — so it always sees the correct tenant without being passed one.
/// In a real app this would be your DbContext, a repository, or domain services.
/// </summary>
public sealed class GreetingService(ITenantContext<string> tenantContext)
{
    public string Greeting() =>
        tenantContext.HasTenant
            ? $"Hello from tenant '{tenantContext.CurrentTenantId}' ({tenantContext.CurrentTenant!.Name})"
            : "Hello from no tenant";
}
