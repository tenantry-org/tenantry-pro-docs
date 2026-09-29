# `TenantJobFilter<TKey>` class

Namespace: `Tenantry.Pro.Hangfire.Filters` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Hangfire filter that propagates the current tenant context into background jobs.

At enqueue time ([`TenantJobFilter<TKey>.OnCreating`](tenantry-pro-hangfire-filters-tenantjobfilter.md)), the active tenant ID is stored as a Hangfire     job parameter. At execution time ([`TenantJobFilter<TKey>.OnPerforming`](tenantry-pro-hangfire-filters-tenantjobfilter.md)), the tenant is looked up     from the store and the scope is restored, so any code running inside the job sees the     correct [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext).

**No-tenant jobs:** If no tenant is active when the job is enqueued, the     parameter is omitted and the job executes without a tenant scope.

**Sync-over-async:** Hangfire server filters are synchronous.     `ITenantStore<TKey>.GetTenantAsync` is called with     `.GetAwaiter().GetResult()`, which blocks the Hangfire worker thread briefly.     This is acceptable because Hangfire workers run on the thread pool — not on ASP.NET     request threads — and tenant stores are typically backed by fast, cached lookups.

Register via `pro.AddHangfireTenantFilter()` and wire into Hangfire using     `app.UseTenantryHangfire<TKey>()`.

```csharp
public sealed class TenantJobFilter<TKey> : IClientFilter, IServerFilter where TKey : IEquatable<TKey>, IParsable<TKey>
```

## Type parameters

- `TKey`: The tenant identifier type.

Implements `IClientFilter`, `IServerFilter`.

## Constructors

### `TenantJobFilter(ITenantContext<TKey>, ITenantScope<TKey>, ITenantStoreAccessor<TKey>, MissingTenantBehavior, ILogger<TenantJobFilter<TKey>>)`

Hangfire filter that propagates the current tenant context into background jobs.

```csharp
public TenantJobFilter(ITenantContext<TKey> tenantContext, ITenantScope<TKey> tenantScope, ITenantStoreAccessor<TKey> storeAccessor, MissingTenantBehavior onMissingTenant, ILogger<TenantJobFilter<TKey>> logger)
```

Parameters:

- `tenantContext` [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext): Supplies the current tenant when a job is created.
- `tenantScope` [`ITenantScope<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantscope): Runs each job under the tenant it was created for.
- `storeAccessor` [`ITenantStoreAccessor<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantstoreaccessor): Looks up the job's tenant before it runs.
- `onMissingTenant` [`MissingTenantBehavior`](https://tenantry.dev/docs/core/api/tenantry-core-missingtenantbehavior): What to do with a job that carries no tenant, or one the store does not know.
- `logger` `ILogger<TenantJobFilter<TKey>>`: Logs jobs that run without a tenant.

At enqueue time ([`TenantJobFilter<TKey>.OnCreating`](tenantry-pro-hangfire-filters-tenantjobfilter.md)), the active tenant ID is stored as a Hangfire     job parameter. At execution time ([`TenantJobFilter<TKey>.OnPerforming`](tenantry-pro-hangfire-filters-tenantjobfilter.md)), the tenant is looked up     from the store and the scope is restored, so any code running inside the job sees the     correct [`ITenantContext<TKey>`](https://tenantry.dev/docs/core/api/tenantry-core-itenantcontext).

**No-tenant jobs:** If no tenant is active when the job is enqueued, the     parameter is omitted and the job executes without a tenant scope.

**Sync-over-async:** Hangfire server filters are synchronous.     `ITenantStore<TKey>.GetTenantAsync` is called with     `.GetAwaiter().GetResult()`, which blocks the Hangfire worker thread briefly.     This is acceptable because Hangfire workers run on the thread pool — not on ASP.NET     request threads — and tenant stores are typically backed by fast, cached lookups.

Register via `pro.AddHangfireTenantFilter()` and wire into Hangfire using     `app.UseTenantryHangfire<TKey>()`.

## Methods

### `OnCreated(CreatedContext)`

Called after the creation of the job.

```csharp
public void OnCreated(CreatedContext filterContext)
```

Parameters:

- `filterContext` `CreatedContext`: The filter context.

### `OnCreating(CreatingContext)`

Called before the creation of the job.

```csharp
public void OnCreating(CreatingContext filterContext)
```

Parameters:

- `filterContext` `CreatingContext`: The filter context.

### `OnPerformed(PerformedContext)`

Called after the performance of the job.

```csharp
public void OnPerformed(PerformedContext filterContext)
```

Parameters:

- `filterContext` `PerformedContext`: The filter context.

### `OnPerforming(PerformingContext)`

Called before the performance of the job.

```csharp
public void OnPerforming(PerformingContext filterContext)
```

Parameters:

- `filterContext` `PerformingContext`: The filter context.
