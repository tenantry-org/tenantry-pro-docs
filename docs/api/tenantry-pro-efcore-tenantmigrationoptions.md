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

Creates the context migrations are applied through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to change the schema, when the application's own are not; the context is disposed after use. When not set, the context is the application's `TContext` from the tenant's scope, as a request gets it, or, when that cannot be created or has no connection string yet (only an asynchronous one), one from its `IDbContextFactory<TContext>`.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`

A run creates each tenant's context to find the database and schema it connects to, then creates it again for the first tenant of each to migrate it.

### `CreateMissingDatabases`

Whether a migration run creates a tenant's database, or with schema per tenant its schema, that does not exist, as EF Core's `Migrate` does. Default: [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool): the run reports that database or schema as failed, and migrates the others.

```csharp
public bool CreateMissingDatabases { get; set; }
```

Value: `bool`

A missing database usually means a mistake: a tenant whose database was dropped (offboarding) but which is     still in the store, or a connection string that names the wrong database. Creating it would give the tenant     an empty, fully migrated database that requests then reach. Create tenants' databases and schemas with     provisioning, whose migration step always creates.

Set it for development, where a new developer's databases do not exist yet:     `o.CreateMissingDatabases = builder.Environment.IsDevelopment()`. It applies to migration runs: the     deployment step, `OnStartup`, and [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md).

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
