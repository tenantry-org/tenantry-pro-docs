using Tenantry;

namespace Tenantry.Pro.Samples.HangfireJobs;

/// <summary>
/// A background job. Hangfire creates it for each run, after Tenantry's filter has made the tenant it was enqueued
/// for current, so its dependencies, scoped ones included, see that tenant.
/// </summary>
public class ReportJob(ReportWriter writer)
{
    public Task GenerateAsync() => writer.WriteAsync();
}

/// <summary>A scoped service, such as one that uses a <c>DbContext</c>, which reads the current tenant.</summary>
public class ReportWriter(ITenantContext<string> tenantContext, ILogger<ReportWriter> logger)
{
    public Task WriteAsync()
    {
        logger.LogInformation("Generating report for tenant '{TenantId}'", tenantContext.CurrentTenantId);
        return Task.CompletedTask;
    }
}
