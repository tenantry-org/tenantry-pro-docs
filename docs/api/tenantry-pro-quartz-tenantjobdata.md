# `TenantJobData` class

Namespace: `Tenantry.Pro.Quartz` · Package: `Tenantry.Pro.Quartz` · [API reference](README.md)

Well-known keys used to carry tenant context through Quartz job data maps.

```csharp
public static class TenantJobData
```

## Fields

### `TenantIdKey`

The `JobDataMap` key under which the tenant ID is stored. Stamp it at schedule time with `new JobDataMap().WithTenant(tenantId)`; the tenant-scoped job factory reads it to run the job inside the tenant's scope.

```csharp
public const string TenantIdKey = "tenantry-tenant-id"
```

Returns: `string`
