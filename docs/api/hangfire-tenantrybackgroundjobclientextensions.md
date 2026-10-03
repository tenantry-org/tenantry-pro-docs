# `TenantryBackgroundJobClientExtensions` class

Namespace: `Hangfire` · Package: `Tenantry.Pro.Hangfire` · [API reference](README.md)

Extension methods for enqueueing a Hangfire job for a tenant other than the current one.

```csharp
public static class TenantryBackgroundJobClientExtensions
```

## Methods

### `WithTenant<TKey>(IBackgroundJobClient, ITenantDescriptor<TKey>)`

Returns a client whose jobs carry `tenant`'s id, so they run as that tenant. Prefer it to the id overload when you hold the tenant: the key type then comes from it, and a value of the wrong type, which would fail when the job runs, does not compile.

```csharp
public static IBackgroundJobClient WithTenant<TKey>(this IBackgroundJobClient client, ITenantDescriptor<TKey> tenant) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `client` `IBackgroundJobClient`: The application's client, such as the `IBackgroundJobClient` it injects.
- `tenant` `ITenantDescriptor<TKey>`: The tenant.

Returns: `IBackgroundJobClient`: A client that creates every job for `tenantId`.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant"; or `client` cannot create a job with parameters (it does not implement `IBackgroundJobClientV2`, as Hangfire's own client does).

The tenant is stored in each job's parameters, under `HeaderName`, which Tenantry's job filter then leaves in place.

```csharp
jobs.WithTenant(tenantId).Enqueue<ReportJob>(job => job.Execute());
```

### `WithTenant<TKey>(IBackgroundJobClient, TKey)`

Returns a client whose jobs carry `tenantId`, so they run as that tenant (`pro.AddHangfirePropagation()`), whichever tenant is current, if any: for enqueueing on a tenant's behalf from code that runs without one, such as an administrator's request. Use it like the client it wraps.

```csharp
public static IBackgroundJobClient WithTenant<TKey>(this IBackgroundJobClient client, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `client` `IBackgroundJobClient`: The application's client, such as the `IBackgroundJobClient` it injects.
- `tenantId` `TKey`: The tenant's id. The tenant is looked up when the job runs.

Returns: `IBackgroundJobClient`: A client that creates every job for `tenantId`.

Exceptions:

- `ArgumentException`: `tenantId` is the key type's default value (`Guid.Empty`, `0`) or an empty string, which Tenantry reserves for "no tenant"; or `client` cannot create a job with parameters (it does not implement `IBackgroundJobClientV2`, as Hangfire's own client does).

The tenant is stored in each job's parameters, under `HeaderName`, which Tenantry's job filter then leaves in place.

```csharp
jobs.WithTenant(tenantId).Enqueue<ReportJob>(job => job.Execute());
```
