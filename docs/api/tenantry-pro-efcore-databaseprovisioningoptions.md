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

Creates the context the database is created through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to create databases, when the application's own are not.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`

The step disposes the context. When not set, the step uses the application's `TContext` from the tenant's scope, as a request gets it. When that is not registered, or has no connection string yet (Tenantry Core reads an asynchronous one when the context first connects), it uses the application's `IDbContextFactory<TContext>`.
