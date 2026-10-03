# Tenantry.Pro.Samples.HangfireJobs

Tenant context in **Hangfire** jobs. A job enqueued during a request carries that request's tenant, and runs in
that tenant's scope, so the job and every service it depends on see the tenant. It uses Hangfire's in-memory
storage, so it runs with no database.

Demonstrates:

- `pro.AddHangfirePropagation()` and `config.UseTenantry(sp)` in `AddHangfire`: a job carries the tenant it was
  enqueued for, and runs as that tenant.
- A job whose scoped dependency (`ReportWriter`) reads `ITenantContext<string>`.

## Run

```bash
dotnet run --project Tenantry.Pro.Samples.HangfireJobs --Tenantry:License "<your-licence-key>"

curl -X POST localhost:5000/jobs/report -H "X-Tenant-Id: acme"
# log: "Generating report for tenant 'acme'"
```

A recurring job added with `RecurringJob.AddOrUpdate` is created by Hangfire's scheduler, with no tenant, so it runs
without one; `AddOrUpdateForEachTenant` runs a recurring job for each tenant. See the
[Hangfire guide](../../docs/hangfire.md#recurring-jobs).
