# `TenantryProEfCoreBuilderExtensions` class

Namespace: `Microsoft.Extensions.DependencyInjection` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Registers Tenantry.Pro's EF Core features on [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md).

```csharp
public static class TenantryProEfCoreBuilderExtensions
```

## Methods

### `AddAuditLogging<TKey>(IProBuilder<TKey>, Action<AuditOptions>?)`

Audit logging: records the changes every context that uses `UseTenantry()` saves, as [`AuditEntry`](tenantry-pro-efcore-auditentry.md) values with the current tenant, and passes them to [`IAuditStore`](tenantry-pro-efcore-iauditstore.md): by default once they are committed ([`AuditOptions.Timing`](tenantry-pro-efcore-auditoptions.md)). The default store writes each entry to the log; register your own [`IAuditStore`](tenantry-pro-efcore-iauditstore.md), with any lifetime, to replace it.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder<TKey> AddAuditLogging<TKey>(this IProBuilder<TKey> pro, Action<AuditOptions>? configure = null) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<AuditOptions>`: Optionally sets [`AuditOptions`](tenantry-pro-efcore-auditoptions.md): entity types and properties to exclude, when entries are written, and what a store failure does.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

Nothing is added to the contexts: `UseTenantry()` adds the audit interceptor, after Tenantry's own, so     a new tenant-owned entity is recorded with its tenant. A context that does not use `UseTenantry()` is     not audited, nor are the saves the store makes.

By default ([`AuditTiming.AfterCommit`](tenantry-pro-efcore-audittiming.md)) the entries of changes saved in a transaction are     written when it commits, a `Database.BeginTransaction` transaction (shared with other contexts through     `UseTransaction` or not), a `TransactionScope` or an enlisted one, and discarded if it rolls back.     A store failure is then logged, as the changes are committed, or thrown as an     [`AuditStoreException`](tenantry-pro-efcore-auditstoreexception.md) ([`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md)). With     [`AuditTiming.InTransaction`](tenantry-pro-efcore-audittiming.md) the store is called inside the transaction, so it can write through     the same connection and transaction, and its failure is thrown from `SaveChanges`.

Each entry says who made the change, and its correlation id, as [`IAuditContextProvider`](tenantry-pro-efcore-iauditcontextprovider.md) gives     them: by default no one, and the current trace's id. Register your own to name the user.

Options that are not valid (an undefined [`AuditOptions.Timing`](tenantry-pro-efcore-auditoptions.md) or     [`AuditOptions.OnStoreFailure`](tenantry-pro-efcore-auditoptions.md)) stop the application from starting.

```csharp
tenant.UsePro(pro => pro.AddAuditLogging());
builder.Services.AddScoped<IAuditStore, MyDatabaseAuditStore>();
```

### `AddDatabaseDeprovisioning<TContext>(IProBuilder, Action<DatabaseDeprovisioningOptions<TContext>>?)`

Adds dropping each tenant's database to offboarding ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)), as the `DropDatabase` step, after the application's deprovisioning steps and `DeleteSharedData`. It drops the database `TContext` connects to for the tenant, through EF Core's database creator (on SQL Server, after ending other sessions in it). In mixed mode it applies only to [`TenantIsolation.Database`](tenantry-pro-tenantisolation.md) tenants.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder AddDatabaseDeprovisioning<TContext>(this IProBuilder pro, Action<DatabaseDeprovisioningOptions<TContext>>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context whose database is dropped, with any relational EF Core provider.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.
- `configure` `Action<DatabaseDeprovisioningOptions<TContext>>`: Optionally sets how the context is created ([`DatabaseDeprovisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-databasedeprovisioningoptions.md)).

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

It is refused when another tenant's context connects to a database of the same name, unless that database says it is another one (SQL Server's database GUID, PostgreSQL's `system_identifier`, MySQL's `server_uuid`), and when another tenant's context cannot be created, or its database asked, to check. A database that does not exist counts as dropped. On PostgreSQL, connections other instances of the application hold open make the drop fail; offboarding again succeeds once they close.

```csharp
tenant.UsePro(pro => pro.AddDatabaseProvisioning<AppDbContext>().AddDatabaseDeprovisioning<AppDbContext>());
```

### `AddDatabaseProvisioning<TContext>(IProBuilder, Action<DatabaseProvisioningOptions<TContext>>?)`

Adds creating each tenant's database to tenant provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)), as the `CreateDatabase` step, which runs first. It creates the database `TContext` connects to for the tenant, through EF Core's database creator, unless it exists. In mixed mode it applies only to [`TenantIsolation.Database`](tenantry-pro-tenantisolation.md) tenants.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder AddDatabaseProvisioning<TContext>(this IProBuilder pro, Action<DatabaseProvisioningOptions<TContext>>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context whose database is created, with any relational EF Core provider.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.
- `configure` `Action<DatabaseProvisioningOptions<TContext>>`: Optionally sets how the context is created ([`DatabaseProvisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-databaseprovisioningoptions.md)).

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

The step uses `TContext` from the tenant's scope, so with Tenantry Core's `AddDbContextPerTenantDatabase` it creates the tenant's own database. Its credentials must be allowed to create databases, or set [`DatabaseProvisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-databaseprovisioningoptions.md). Two runs for one tenant at once both succeed: the one whose create fails waits, up to 30 seconds, for the database to come online, and logs the failure as a warning, in case no other run created it.

### `AddMigrations<TContext>(IProBuilder, Action<TenantMigrationOptions<TContext>>?)`

Applies `TContext`'s EF Core migrations for every tenant: registers [`ITenantMigrationRunner<TKey>`](tenantry-pro-efcore-itenantmigrationrunner.md), adds the `Migrations` step to tenant provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)), after the database or schema is created, and optionally applies them when the application starts ([`TenantMigrationOptions<TContext>.OnStartup`](tenantry-pro-efcore-tenantmigrationoptions.md)).

```csharp
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
public static IProBuilder AddMigrations<TContext>(this IProBuilder pro, Action<TenantMigrationOptions<TContext>>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context whose migrations are applied.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.
- `configure` `Action<TenantMigrationOptions<TContext>>`: Optionally sets when migrations run at startup, and how the context is created.

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

The context comes from the application's registration, in each tenant's scope, so it connects to the     tenant's database (Tenantry Core's `AddDbContextPerTenantDatabase`) and, with schema per tenant, uses the     tenant's schema. Tenants whose context connects to the same database and schema are migrated once: in mixed     mode, the shared database. Call it once for each context to migrate.

With schema per tenant (`UseSchemaPerTenant`), the migrations are generated without a schema (the     design-time model has none) and applied to each tenant's schema, with the migration history table in that     schema. SQL a migration runs with `migrationBuilder.Sql` is applied as written.

In mixed mode the provisioning step applies to [`TenantIsolation.Database`](tenantry-pro-tenantisolation.md) and     [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants; the shared database is migrated with every tenant, by the     runner.

```csharp
tenant.UsePro(pro => pro.AddMigrations<AppDbContext>());
```

### `AddSchemaDeprovisioning<TContext>(IProBuilder, Action<SchemaDeprovisioningOptions<TContext>>?)`

Adds dropping each tenant's schema to offboarding ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)), as the `DropSchema` step, after the application's deprovisioning steps and `DeleteSharedData`: its foreign keys, tables (the migration history among them) and sequences, then the schema, in one transaction, in the database `TContext` connects to for the tenant. In mixed mode it applies only to [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder AddSchemaDeprovisioning<TContext>(this IProBuilder pro, Action<SchemaDeprovisioningOptions<TContext>>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context, on SQL Server or PostgreSQL, whose database the schema is dropped from.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.
- `configure` `Action<SchemaDeprovisioningOptions<TContext>>`: Optionally sets how the context is created ([`SchemaDeprovisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-schemadeprovisioningoptions.md)).

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

It needs `pro.UseSchemaPerTenant(...)`: without it, the application does not start. It is refused when another tenant on the same database uses the schema (its schema is that one, or its model maps anything into it, directly or through its connection's default schema), when the schema is the database's default schema, and when another tenant's context cannot be created, or what it uses read, to check. A schema that does not exist counts as dropped. Objects other than tables and sequences (views, functions) stay, and dropping the schema then fails: drop them in a deprovisioning step of your own.

### `AddSchemaProvisioning<TContext>(IProBuilder, Action<SchemaProvisioningOptions<TContext>>?)`

Adds creating each tenant's schema to tenant provisioning ([`ITenantProvisioner<TKey>`](tenantry-pro-itenantprovisioner.md)), as the `CreateSchema` step, which runs first. It creates the tenant's schema (`UseSchemaPerTenant`), unless it exists, in the database `TContext` connects to for the tenant. In mixed mode it applies only to [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder AddSchemaProvisioning<TContext>(this IProBuilder pro, Action<SchemaProvisioningOptions<TContext>>? configure = null) where TContext : DbContext
```

Type parameters:

- `TContext`: The context, on SQL Server or PostgreSQL, whose database the schema is created in.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.
- `configure` `Action<SchemaProvisioningOptions<TContext>>`: Optionally sets how the context is created ([`SchemaProvisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-schemaprovisioningoptions.md)).

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

It needs `pro.UseSchemaPerTenant(...)`: without it, the application does not start. The step creates the schema with EF Core's migrations SQL, so only SQL Server and PostgreSQL are supported (MySQL has no schemas apart from databases); another provider fails the step with `NotSupportedException`. A schema name too long for the database, or with control characters, fails it with `InvalidOperationException`. Its credentials must be allowed to create schemas, or set [`SchemaProvisioningOptions<TContext>.CreateContext`](tenantry-pro-efcore-schemaprovisioningoptions.md).

### `AddSharedDataDeletion<TContext>(IProBuilder)`

Adds deleting a tenant's rows from the shared database to offboarding ([`ITenantDeprovisioner<TKey>`](tenantry-pro-itenantdeprovisioner.md)), as the `DeleteSharedData` step, after the application's deprovisioning steps and before any drop: every row of `TContext`'s tenant-owned entities (`ITenantEntity<TKey>`) with the tenant's id, table by table, rows that reference another's first, in one transaction. In mixed mode it applies to [`TenantIsolation.Shared`](tenantry-pro-tenantisolation.md) tenants, and to [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants when schema per tenant leaves `TContext` in the shared schema ([`SchemaPerTenantOptions<TKey>.Contexts`](tenantry-pro-efcore-schemapertenantoptions.md)), and to [`TenantIsolation.Database`](tenantry-pro-tenantisolation.md) tenants unless `AddDatabaseDeprovisioning` drops `TContext`'s database.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder AddSharedDataDeletion<TContext>(this IProBuilder pro) where TContext : DbContext
```

Type parameters:

- `TContext`: The context, on the shared database, whose tenant-owned tables are cleared.

Parameters:

- `pro` [`IProBuilder`](tenantry-pro-iprobuilder.md): The Pro builder.

Returns: [`IProBuilder`](tenantry-pro-iprobuilder.md): The same builder, without its key type: in a chain, call it after methods that need the key type.

Rows of tables that cascade from those (owned types in tables of their own, many-to-many links) go with them, through the database's cascades. A table that is not tenant-owned and references a tenant's row makes the delete fail, and nothing is deleted; so does a cycle of references between tenant-owned tables, and a context with no tenant-owned table. Add the context once for each database whose rows go.

```csharp
tenant.UsePro(pro => pro.AddSharedDataDeletion<AppDbContext>());
```

### `UseSchemaPerTenant<TKey>(IProBuilder<TKey>, Action<SchemaPerTenantOptions<TKey>>)`

Schema per tenant: each tenant's tables in a schema of its own. The context that uses `UseTenantry()`, or with more than one those [`SchemaPerTenantOptions<TKey>.Contexts`](tenantry-pro-efcore-schemapertenantoptions.md) lists, gets the current tenant's schema as its default schema, with a compiled model per schema; nothing in the context or its registration names the schema.

```csharp
[RequiresUnreferencedCode("EF Core reads entity types and their properties through reflection, which trimming can break. See https://aka.ms/efcore-docs-trimming.")]
[RequiresDynamicCode("EF Core builds its model and queries at run time, which Native AOT does not support.")]
public static IProBuilder<TKey> UseSchemaPerTenant<TKey>(this IProBuilder<TKey> pro, Action<SchemaPerTenantOptions<TKey>> configure) where TKey : IEquatable<TKey>, IParsable<TKey>
```

Type parameters:

- `TKey`: The tenant identifier type.

Parameters:

- `pro` [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The Pro builder.
- `configure` `Action<SchemaPerTenantOptions<TKey>>`: Sets [`SchemaPerTenantOptions<TKey>.GetSchemaName`](tenantry-pro-efcore-schemapertenantoptions.md), and optionally the cache sizes.

Returns: [`IProBuilder<TKey>`](tenantry-pro-iprobuilder-1.md): The same `pro` for chaining.

The schema is the model's default schema, set after `OnModelCreating`, so it applies to every table     without a schema of its own. Without a current tenant (design-time tools such as `dotnet ef`, say) the     model has no default schema, so migrations are generated without one. In mixed mode only     [`TenantIsolation.Schema`](tenantry-pro-tenantisolation.md) tenants get a schema; the others keep the database's default.

A context's first command or save throws `TenantNotResolvedException` without a current     tenant, and [`TenantIsolationViolationException`](https://tenantry.dev/docs/core/api/tenantry-efcore-tenantisolationviolationexception) under a tenant other than the one whose schema     its model was built for.

The contexts cannot be pooled: a pooled context keeps the model of the first tenant it served. Creating one     with `AddDbContextPool`, `AddPooledDbContextFactory` or Tenantry Core's     `AddDbContextPerTenantDatabase` with `pooled: true` throws `InvalidOperationException`.     Each context type also gets a model cache sized for [`SchemaPerTenantOptions<TKey>.MaxCachedSchemas`](tenantry-pro-efcore-schemapertenantoptions.md)     schemas, in place of EF Core's, in an EF Core internal service provider of its own that the applications in     the process share; EF Core throws once a process has built more than 20 of them. `UseMemoryCache`, or     `ReplaceService` of `IModelCacheKeyFactory` or `IMemoryCache`, on their options throws     `InvalidOperationException`.

Options that are not valid (no `GetSchemaName`, a cache size out of range) stop the application from     starting, as does more than one context type that uses `UseTenantry()` with none listed in     [`SchemaPerTenantOptions<TKey>.Contexts`](tenantry-pro-efcore-schemapertenantoptions.md).

```csharp
tenant.UsePro(pro => pro.UseSchemaPerTenant(o => o.GetSchemaName = t => $"tenant_{t.TenantId}"));
```
