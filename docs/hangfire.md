# Hangfire Integration

Use this package to propagate the current tenant context into Hangfire background jobs.
When a job is enqueued during an active tenant request, the tenant ID is stored as a job
parameter. When the job executes on a Hangfire worker, the tenant scope is restored so that
any code running inside the job sees the correct `ITenantContext<TKey>`.

## What Tenantry.Pro.Hangfire Provides

- `TenantJobFilter<TKey>` — Hangfire `IClientFilter` + `IServerFilter` that captures and restores the tenant context
- `pro.AddHangfireTenantFilter()` — registers the filter in DI
- `app.UseTenantryHangfire<TKey>()` — adds the filter to Hangfire's `GlobalJobFilters`

## Requirements

- `Tenantry.Pro.Hangfire` NuGet package
- Hangfire 1.8+

## Registration

```csharp
using Tenantry.Pro;
using Tenantry.Pro.Hangfire.Extensions;

// 1. Register DI services
builder.Services.AddTenantry<string>(tenant =>
{
    tenant.ResolveFromHeader("X-Tenant-Id");
    tenant.UseStore<MyTenantStore>();
    tenant.UsePro(pro =>
    {
        pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
        pro.AddHangfireTenantFilter();   // registers TenantJobFilter<TKey> in DI
    });
});

// 2. Configure Hangfire (your transport of choice)
builder.Services.AddHangfire(config => config.UseMemoryStorage());
builder.Services.AddHangfireServer();

var app = builder.Build();

// 3. Wire the filter into Hangfire's GlobalJobFilters
app.UseTenantryHangfire<string>();

app.UseHangfireDashboard();
app.Run();
```

## Behaviour

| Scenario | Result |
|----------|--------|
| Job enqueued during active tenant request | Tenant ID stored as job parameter; scope restored at execution |
| Job enqueued outside a tenant scope | No parameter stored; the missing-tenant policy applies (default: warn, run without scope) |
| Job parameter present but tenant not in store | The missing-tenant policy applies (default: warn, run without scope) |

## Missing-tenant policy

When a job has no resolvable tenant — none was stamped, the stamped value can't be parsed as `TKey`,
or the tenant isn't in the store — `AddHangfireTenantFilter` decides what to do via
`TenantPropagationOptions.OnMissingTenant`:

```csharp
pro.AddHangfireTenantFilter(o => o.OnMissingTenant = MissingTenantBehavior.Skip);
```

| `MissingTenantBehavior` | Effect |
|-------------------------|--------|
| `Allow` | Run the job without a tenant scope, silently. |
| `Warn` *(default)* | Run the job without a tenant scope, log a warning. |
| `Reject` | Throw — Hangfire records the job as failed and applies its retry policy. |
| `Skip` | Cancel the job (Hangfire marks it completed) without running the body. |

Use `Allow` for deployments with legitimately global jobs; use `Reject`/`Skip` to make an
unexpectedly-untenanted job fail loudly or drop rather than run cross-tenant. The same option exists on
every Tenantry.Pro integration (MassTransit, Quartz, Rebus) and mirrors core's
`EfCoreIsolationOptions.OnMissingTenant`.

## Accessing the tenant inside a job

```csharp
public class ReportJob(ITenantContext<string> tenantContext)
{
    public void Execute()
    {
        // tenantContext.CurrentTenantId is set to the tenant that enqueued the job
        var id = tenantContext.CurrentTenantId;
    }
}
```

## Limitations

- Hangfire server filters are synchronous. The tenant store lookup uses `.GetAwaiter().GetResult()`,
  which blocks the Hangfire worker thread briefly. This is acceptable because Hangfire workers run
  on background threads, not on ASP.NET request threads.
- Jobs enqueued outside a tenant scope (e.g. from admin background tasks) execute without a scope.
  Inject `ITenantContext<TKey>` and check `HasTenant` if your job must handle both cases.
