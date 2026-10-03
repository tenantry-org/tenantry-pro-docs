# `DatabaseProvisioningOptions<TContext>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

How tenant provisioning creates each tenant's database. Set with `pro.AddDatabaseProvisioning<TContext>(o => …)`.

```csharp
public sealed class DatabaseProvisioningOptions<TContext> where TContext : DbContext
```

## Type parameters

- `TContext`: The context whose database is created.

## Properties

### `CreateContext`

Creates the context the database is created through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to create databases, when the application's own are not; the step disposes the context. When not set, the context comes from the application's registration: its `IDbContextFactory<TContext>`, if it has one, otherwise `TContext` from the tenant's scope.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`
