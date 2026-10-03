# Tenantry.Pro.Samples.QuartzScheduling

Tenant context in **Quartz.NET** jobs. A job scheduled for a tenant runs as that tenant, so the job and every
service it depends on see the tenant, and a recurring job marked for each tenant runs once for every tenant. It uses
Quartz's in-memory job store, so it runs with no database.

Demonstrates:

- `pro.AddQuartzPropagation()` and `q.UseTenantry()` in `AddQuartz`: Tenantry's job factory makes a job's tenant
  current, then creates the job.
- `JobDataMap().WithTenant(id)`: `POST /jobs/report` schedules a job for the request's tenant. Quartz stores a job
  without the tenant that was current when it was scheduled, so the endpoint passes it in the job's data.
- `JobDataMap().ForEachTenant()`: the recurring `cleanup` job, which fires at startup and then every minute, runs once
  for each tenant in the store.
- Jobs whose scoped dependency (`ReportWriter`) reads `ITenantContext<string>`.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.QuartzScheduling --Tenantry:License "<your-licence-key>"
# log: "Cleaning up expired sessions for tenant 'acme'", and the same for 'globex'

curl -X POST localhost:5000/jobs/report -H "X-Tenant-Id: acme"
# log: "Generating report for tenant 'acme'"
```

See the [Quartz.NET guide](../../docs/quartz.md).
