# `DatabaseDeprovisioningOptions<TContext>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

How offboarding drops each tenant's database. Set with `pro.AddDatabaseDeprovisioning<TContext>(o => …)`.

```csharp
public sealed class DatabaseDeprovisioningOptions<TContext> where TContext : DbContext
```

## Type parameters

- `TContext`: The context whose database is dropped.

## Properties

### `CreateContext`

Creates the context the database is dropped through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to drop databases, when the application's own are not; the step disposes the context. It also creates each other tenant's context, to check none of them uses the database. When not set, the context comes from the application's registration: its `IDbContextFactory<TContext>`, if it has one, otherwise `TContext` from the tenant's scope.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`
