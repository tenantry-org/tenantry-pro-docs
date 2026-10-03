# `TenantryRecurringJobManagerExtensions` class

Namespace: `Hangfire` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for a recurring Hangfire job that runs for each tenant.

```csharp
public static class TenantryRecurringJobManagerExtensions
```

## Methods

### `AddOrUpdateForEachTenant<T>(IRecurringJobManager, string, Expression<Action<T>>, string, RecurringJobOptions?)`

Adds or updates a recurring job that, each time it is due, enqueues `methodCall` once for each tenant in the store, and each of those jobs runs as its tenant (`pro.AddHangfirePropagation()`).

```csharp
public static void AddOrUpdateForEachTenant<T>(this IRecurringJobManager manager, string recurringJobId, Expression<Action<T>> methodCall, string cronExpression, RecurringJobOptions? options = null)
```

Type parameters:

- `T`: The type whose method the job calls, created by Hangfire's job activator.

Parameters:

- `manager` `IRecurringJobManager`: The application's recurring job manager, such as the `IRecurringJobManager` it injects.
- `recurringJobId` `string`: The recurring job's id.
- `methodCall` `Expression<Action<T>>`: The job to run for each tenant.
- `cronExpression` `string`: When the job is due, such as `Cron.Daily()`.
- `options` `RecurringJobOptions`: The recurring job's options, such as its time zone.

The recurring job itself runs without a tenant, whatever `OnMissingTenant` is: it reads every tenant     from the store (`ITenantLookup`) and enqueues one job for each, so each tenant's job is retried     and shown on its own. If the store cannot be read, the recurring job fails and Hangfire retries it.

The tenants' jobs go to the default queue, or the one a `QueueAttribute` on the method names.     The recurring job does not check a tenant's status, so a job must skip a tenant your application has     suspended.

```csharp
recurringJobs.AddOrUpdateForEachTenant<ReportJob>("nightly-reports", job => job.ExecuteAsync(), Cron.Daily());
```

### `AddOrUpdateForEachTenant<T>(IRecurringJobManager, string, Expression<Func<T, Task>>, string, RecurringJobOptions?)`

Adds or updates a recurring job that, each time it is due, enqueues `methodCall` once for each tenant in the store, and each of those jobs runs as its tenant (`pro.AddHangfirePropagation()`).

```csharp
public static void AddOrUpdateForEachTenant<T>(this IRecurringJobManager manager, string recurringJobId, Expression<Func<T, Task>> methodCall, string cronExpression, RecurringJobOptions? options = null)
```

Type parameters:

- `T`: The type whose method the job calls, created by Hangfire's job activator.

Parameters:

- `manager` `IRecurringJobManager`: The application's recurring job manager, such as the `IRecurringJobManager` it injects.
- `recurringJobId` `string`: The recurring job's id.
- `methodCall` `Expression<Func<T, Task>>`: The job to run for each tenant.
- `cronExpression` `string`: When the job is due, such as `Cron.Daily()`.
- `options` `RecurringJobOptions`: The recurring job's options, such as its time zone.

The recurring job itself runs without a tenant, whatever `OnMissingTenant` is: it reads every tenant     from the store (`ITenantLookup`) and enqueues one job for each, so each tenant's job is retried     and shown on its own. If the store cannot be read, the recurring job fails and Hangfire retries it.

The tenants' jobs go to the default queue, or the one a `QueueAttribute` on the method names.     The recurring job does not check a tenant's status, so a job must skip a tenant your application has     suspended.

```csharp
recurringJobs.AddOrUpdateForEachTenant<ReportJob>("nightly-reports", job => job.ExecuteAsync(), Cron.Daily());
```
