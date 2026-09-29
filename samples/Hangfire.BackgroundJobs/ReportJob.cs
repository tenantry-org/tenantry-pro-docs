using Tenantry.Core;

namespace Hangfire.BackgroundJobs;

/// <summary>
/// Example background job. When Tenantry's Hangfire filter is active, <see cref="ITenantContext{TKey}"/>
/// is automatically populated with the tenant that enqueued the job — no manual plumbing required.
/// </summary>
public class ReportJob(ITenantContext<string> tenantContext, ILogger<ReportJob> logger)
{
    public Task GenerateAsync()
    {
        logger.LogInformation(
            "Generating report for tenant '{TenantId}'",
            tenantContext.CurrentTenantId);

        // Your per-tenant logic here — inject scoped services, DbContext, etc.

        return Task.CompletedTask;
    }
}
