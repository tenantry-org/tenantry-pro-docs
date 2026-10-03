# Database providers

Tenantry.Pro has no provider packages: `Tenantry.Pro.EfCore` works with the EF Core provider your context
already uses. Provisioning creates a tenant's database through that provider's own database creator, and a
tenant's schema with that provider's own migrations SQL, so names are quoted the way the provider quotes
them.

## Capability matrix

| Capability | SQL Server | PostgreSQL | MySQL / MariaDB | Other relational providers |
|------------|:---:|:---:|:---:|:---:|
| Database per tenant (Tenantry core) | ✅ | ✅ | ✅ | ✅ |
| Database provisioning (`AddDatabaseProvisioning<TContext>()`) | ✅ | ✅ | ✅ | ✅ (not tested) |
| Schema per tenant (`UseSchemaPerTenant`) | ✅ | ✅ | ❌ | ➖ |
| Schema provisioning (`AddSchemaProvisioning<TContext>()`) | ✅ | ✅ | ❌ | ❌ |
| Migrations, database per tenant (`AddMigrations<TContext>()`) | ✅ | ✅ | ✅ | ✅ (not tested) |
| Migrations, schema per tenant (`AddMigrations<TContext>()`) | ✅ | ✅ | ❌ | ➖ |

❌ for MySQL: a MySQL "schema" *is* a database, so there is no schema to isolate within one (below).
➖ Schema per tenant, and its migrations, work with any provider that supports EF Core's default schema; only SQL
Server and PostgreSQL are tested, and schema provisioning refuses the others with `NotSupportedException`.

## SQL Server

```bash
dotnet add package Tenantry.Pro.EfCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Supports both strategies. Creating a database needs the `CREATE ANY DATABASE` permission or the `dbcreator` role,
and creating a schema the `CREATE SCHEMA` permission in the shared database.

## PostgreSQL

```bash
dotnet add package Tenantry.Pro.EfCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Supports both strategies. Creating a database needs the `CREATEDB` attribute, and creating a schema the
`CREATE` privilege on the shared database. A schema name longer than 63 bytes is refused, because
PostgreSQL would otherwise shorten it without an error.

## MySQL / MariaDB

```bash
dotnet add package Tenantry.Pro.EfCore

# plus an EF Core provider for your EF Core version:
dotnet add package MySql.EntityFrameworkCore           # EF Core 10 (.NET 10): Oracle's provider
dotnet add package Pomelo.EntityFrameworkCore.MySql    # EF Core 8 or 9
```

**Database-per-tenant only.** In MySQL a "schema" is a database, so there is no schema to isolate within one. Use a database per tenant; schema provisioning
fails with `NotSupportedException`. Creating a database needs the global `CREATE` privilege.

**Choosing the EF Core provider.** Use the combinations that are tested (below). MariaDB is not tested: Pomelo
supports it on EF Core 8 and 9, and Oracle's provider does not.

- **EF Core 8 or 9:** Pomelo, `options.UseMySql(cs, ServerVersion.AutoDetect(cs))`.
- **EF Core 10 (.NET 10):** Oracle's `MySql.EntityFrameworkCore`, `options.UseMySQL(cs)`. Pomelo has no
  EF Core 10 release.

Oracle's provider on EF Core 8 or 9, and either provider on EF Core 11 (.NET 11), are not yet tested. The
[MySQL sample](../samples/Tenantry.Pro.Samples.DatabasePerTenantMySql) runs on .NET 10 with Oracle's provider.

## Tested combinations

Each suite runs against a real database in a container, on every target framework. It covers provisioning (fresh,
repeated, eight concurrent runs, and with `CreateContext`), schema provisioning, schema per tenant and mixed mode on
SQL Server and PostgreSQL (refused on MySQL), and migrations (several tenants, a rerun, a failing tenant later
repaired, and two concurrent runners). Last run: 1 October 2026.

| Database | EF Core provider | .NET / EF Core | Result | Concurrent runners |
|----------|------------------|----------------|--------|--------------------|
| SQL Server 2022 | `Microsoft.EntityFrameworkCore.SqlServer` 8.0.31 / 9.0.20 / 10.0.12 | 8, 9, 10 | Passing | Serialised on EF Core 9 and 10; race on EF Core 8 |
| PostgreSQL 16 | `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 / 9.0.0 / 10.0.3 | 8, 9, 10 | Passing | Serialised on EF Core 9; on EF Core 10 serialised per migration (runs can take turns between migrations, and one then fails); race on EF Core 8 |
| MySQL 8.4 | `Pomelo.EntityFrameworkCore.MySql` 8.0.2 / 9.0.0 | 8, 9 | Passing | Serialised on EF Core 9; race on EF Core 8 |
| MySQL 8.4 | `MySql.EntityFrameworkCore` (Oracle) 10.0.9 | 10 | Passing | Serialised |

"Race" means both runners try to apply the same migration and one of them reports that tenant as failed
("already exists"); the other applies it, the end state is correct, and a later run applies nothing.
"Serialised" means every run reported every tenant as succeeded. For MySQL this was observed repeatedly
rather than documented by the providers. See
[Multiple instances](migration-orchestration.md#multiple-instances).

## Target frameworks

The Pro packages multi-target **net8.0, net9.0, and net10.0** (.NET 8 and 9 as legacy). Pick provider/EF
Core package versions that match your target framework (EF Core 8 for net8.0, etc.), exactly as you would
in any EF Core app. The supported versions and dependency ranges are in [Compatibility](compatibility.md).

## Another relational provider

`AddDatabaseProvisioning<TContext>()` works with any relational EF Core provider whose database creator can
create a database, untested. For anything else, such as a schema on another engine, add a provisioning step
of your own (`ITenantProvisioningStep<TKey>`, added with `pro.AddProvisioningStep<T>()`; see
[Tenant lifecycle](tenant-lifecycle.md#writing-a-step)) that creates what the tenant needs unless it exists.
Your steps run after Tenantry's own, including the `Migrations` step of `AddMigrations`, so a
step that creates the tenant's database or schema should apply the migrations too, for example with
`Database.MigrateAsync()` on a context it resolves from `context.Scope.ServiceProvider`.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md)
- [Tenant migrations](migration-orchestration.md) · [Health checks](health-checks.md)
