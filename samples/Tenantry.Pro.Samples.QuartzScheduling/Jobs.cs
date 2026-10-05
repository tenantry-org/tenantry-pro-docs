using Quartz;

namespace Tenantry.Pro.Samples.QuartzScheduling;

/// <summary>
/// A job scheduled for one tenant. Tenantry's job factory makes the job's tenant current and then creates the job,
/// so its dependencies, scoped ones included, see that tenant.
/// </summary>
public sealed class ReportJob(ReportWriter writer) : IJob
{
    public Task Execute(IJobExecutionContext context) => writer.WriteAsync();
}

/// <summary>A recurring job marked to run for each tenant: each run sees its own tenant.</summary>
public sealed class CleanupJob(ITenantContext<string> tenantContext, ILogger<CleanupJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("Cleaning up expired sessions for tenant '{TenantId}'", tenantContext.CurrentTenantId);
        return Task.CompletedTask;
    }
}

/// <summary>A scoped service, such as one that uses a <c>DbContext</c>, which reads the current tenant.</summary>
public sealed class ReportWriter(ITenantContext<string> tenantContext, ILogger<ReportWriter> logger)
{
    public Task WriteAsync()
    {
        logger.LogInformation("Generating report for tenant '{TenantId}'", tenantContext.CurrentTenantId);
        return Task.CompletedTask;
    }
}
