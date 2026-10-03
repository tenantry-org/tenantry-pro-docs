# `TenantMigrationOptions<TContext>` class

Namespace: `Tenantry.Pro.EfCore` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

How a context's migrations are applied. Set with `pro.AddMigrations<TContext>(o => …)`.

```csharp
public sealed class TenantMigrationOptions<TContext> where TContext : DbContext
```

## Type parameters

- `TContext`: The context whose migrations are applied.

## Properties

### `CreateContext`

Creates the context migrations are applied through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to change the schema, when the application's own are not; the context is disposed after use. When not set, the context comes from the application's registration: its `IDbContextFactory<TContext>`, if it has one, otherwise `TContext` from the tenant's scope.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`

A run creates each tenant's context to find the database and schema it connects to, then creates it again for the first tenant of each to migrate it.

### `MaxConcurrency`

How many databases or schemas are migrated at once. Default: 1, one after another, in the tenant store's order.

```csharp
public int MaxConcurrency { get; set; }
```

Value: `int`

Above 1, tenants' contexts are created and migrated in parallel, each in its tenant's own scope, so the     connection-string provider and [`TenantMigrationOptions<TContext>.CreateContext`](tenantry-pro-efcore-tenantmigrationoptions.md) are called concurrently. Contexts are created one     at a time until one has been created: some EF Core providers set up shared state the first time a context is     used, without a lock.

It shortens a run most with a database per tenant, spread over servers. Schemas in one database may wait     for each other: EF Core 9 and later lock the database while migrating, on providers that support it.

### `OnStartup`

Whether to apply the migrations when the application starts. Default: [`StartupMigrations.None`](tenantry-pro-efcore-startupmigrations.md).

```csharp
public StartupMigrations OnStartup { get; set; }
```

Value: [`StartupMigrations`](tenantry-pro-efcore-startupmigrations.md)

Every instance of the application applies them as it starts, so with more than one instance prefer running them once per deployment, with `RunTenantMigrationsIfRequestedAsync`.
