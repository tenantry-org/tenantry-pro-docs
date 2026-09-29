# `JobDataMapExtensions` class

Namespace: `Tenantry.Pro.Quartz.Extensions` · Package: `Tenantry.Pro.Quartz` · [API reference](README.md)

Extension methods for `JobDataMap` to support tenant propagation.

```csharp
public static class JobDataMapExtensions
```

## Methods

### `WithTenant<TKey>(JobDataMap, TKey)`

Stamps the given tenant ID into the job data map so the tenant-scoped job factory can run the job inside the tenant's scope at execution time.

```csharp
public static JobDataMap WithTenant<TKey>(this JobDataMap map, TKey tenantId) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `map` `JobDataMap`: The job data map to stamp.
- `tenantId` `TKey`: The tenant ID to store.

Returns: `JobDataMap`: The same `map` for chaining.
