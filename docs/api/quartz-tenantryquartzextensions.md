# `TenantryQuartzExtensions` class

Namespace: `Quartz` · Package: `Tenantry.Pro.Quartz` · [API reference](README.md)

Extension methods for wiring Tenantry.Pro's Quartz.NET integration into Quartz, and for scheduling a job for a tenant.

```csharp
public static class TenantryQuartzExtensions
```

## Methods

### `ForEachTenant(JobDataMap)`

Marks the job data so that each time the job fires without a tenant, it runs once for each tenant in the store, each run as its tenant (`pro.AddQuartzPropagation()`): for recurring work per tenant, on one schedule.

```csharp
public static JobDataMap ForEachTenant(this JobDataMap map)
```

Parameters:

- `map` `JobDataMap`: The job data, as given to `UsingJobData`.

Returns: `JobDataMap`: The same `map` for chaining.

When the job fires, Tenantry reads every active tenant from the store (`ITenantLookup`) and schedules     a run of the job for each, starting now, with the job data and priority of the trigger that fired and the     tenant's id, so each tenant's run is executed, and can fail, on its own. The firing that schedules them     does not create or run the job (job listeners still see it as an execution), and fails if the store     cannot be read.

A run whose job data also names a tenant ([`TenantryQuartzExtensions.WithTenant<TKey>`](quartz-tenantryquartzextensions.md)) runs as that     tenant only. A tenant `ValidateTenantActivity` refuses gets no run.

```csharp
q.AddJob<NightlyReportJob>(job => job
    .WithIdentity("nightly-reports")
    .UsingJobData(new JobDataMap().ForEachTenant()));
q.AddTrigger(trigger => trigger.ForJob("nightly-reports").WithCronSchedule("0 0 2 * * ?"));
```

### `UseTenantry(IServiceCollectionQuartzConfigurator)`

Sets Tenantry's job factory, which runs each job as the tenant in its job data, and creates the job with Quartz's DI job factory once that tenant is current. Requires `pro.AddQuartzPropagation()`.

```csharp
public static void UseTenantry(this IServiceCollectionQuartzConfigurator quartz)
```

Parameters:

- `quartz` `IServiceCollectionQuartzConfigurator`: Quartz's configuration, in `services.AddQuartz(q => …)`.

It replaces the job factory, so set no other job factory after it. It can be called before or after `UsePro`.

### `WithTenant<TKey>(JobDataMap, ITenantDescriptor<TKey>)`

Stores `tenant`'s id in the job data, so the job runs as that tenant. Prefer it to the id overload when you hold the tenant: the key type then comes from it, and a value of the wrong type, which would fail when the job runs, does not compile.

```csharp
public static JobDataMap WithTenant<TKey>(this JobDataMap map, ITenantDescriptor<TKey> tenant) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `map` `JobDataMap`: The job data, as given to `UsingJobData`.
- `tenant` `ITenantDescriptor<TKey>`: The tenant.

Returns: `JobDataMap`: The same `map` for chaining.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

Quartz.NET has no point at which a job or trigger can be given the current tenant as it is scheduled, so a job runs as a tenant only when its job data names one: to schedule a job for the current tenant, pass `ITenantContext<TKey>.CurrentTenantId`.

### `WithTenant<TKey>(JobDataMap, TKey)`

Stores `tenantId` in the job data, so the job runs as that tenant (`pro.AddQuartzPropagation()`). It is stored as a string, under `HeaderName`.

```csharp
public static JobDataMap WithTenant<TKey>(this JobDataMap map, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `map` `JobDataMap`: The job data, as given to `UsingJobData`.
- `tenantId` `TKey`: The tenant's id.

Returns: `JobDataMap`: The same `map` for chaining.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant".

Quartz.NET has no point at which a job or trigger can be given the current tenant as it is scheduled, so a job runs as a tenant only when its job data names one: to schedule a job for the current tenant, pass `ITenantContext<TKey>.CurrentTenantId`.
