# Database providers

Tenantry.Pro's strategy logic (resolution, caching, mixed mode, the lifecycle pipeline) is
provider-agnostic. The parts that touch a real database engine — **provisioning** (`CREATE
DATABASE` / `CREATE SCHEMA`) and **migration orchestration** — ship in per-provider packages that use
native ADO.NET with correct identifier escaping.

## Capability matrix

| Capability | `Tenantry.Pro.EfCore.SqlServer` | `Tenantry.Pro.EfCore.Npgsql` | `Tenantry.Pro.EfCore.MySql` |
|------------|:---:|:---:|:---:|
| Database-per-tenant resolution | ✅ | ✅ | ✅ |
| Database provisioning (`AddDatabaseProvisioning`) | ✅ | ✅ | ✅ |
| Schema-per-tenant resolution + model caching | ✅ | ✅ | ➖ |
| Schema provisioning (`AddSchemaProvisioning`) | ✅ | ✅ | ❌ |
| Migration orchestration, database per tenant (`WithMigrationOrchestration`) | ✅ | ✅ | ✅ |
| Migration orchestration, schema per tenant | ❌ | ❌ | ➖ |

➖ Schema-per-tenant resolution itself lives in `Tenantry.Pro` and is engine-neutral, but MySQL has no
separate schema concept (below), so there is nothing to provision. ❌ Migration orchestration covers a
database per tenant only; see [Schema per tenant](schema-per-tenant.md#tables-and-migrations).

## SQL Server

```bash
dotnet add package Tenantry.Pro.EfCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Supports both strategies. Extensions: `AddDatabaseProvisioning()`,
`AddSchemaProvisioning(opts => opts.ConnectionString = ...)`,
`WithMigrationOrchestration<TKey, TContext>(...)`. For a health-check `ConnectionFactory`, use
`cs => new SqlConnection(cs)` (`Microsoft.Data.SqlClient`).

## PostgreSQL

```bash
dotnet add package Tenantry.Pro.EfCore.Npgsql
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

Supports both strategies, with the same extension surface as SQL Server. PostgreSQL has real schemas,
so schema-per-tenant and `AddSchemaProvisioning` work (`CREATE SCHEMA IF NOT EXISTS`). Health-check
`ConnectionFactory`: `cs => new NpgsqlConnection(cs)`.

## MySQL / MariaDB

```bash
dotnet add package Tenantry.Pro.EfCore.MySql

# plus an EF Core provider for your EF Core version:
dotnet add package MySql.EntityFrameworkCore           # EF Core 10 (.NET 10): Oracle's provider
dotnet add package Pomelo.EntityFrameworkCore.MySql    # EF Core 8 or 9
```

**Database-per-tenant only.** In MySQL a "schema" *is* a database — the two terms are synonyms — so
there is no separate schema to isolate within a database. Use a database per tenant. The package
therefore exposes `AddDatabaseProvisioning()` and `WithMigrationOrchestration<TKey, TContext>(...)`
but **no** `AddSchemaProvisioning`.

**Choosing the EF Core provider.** `Tenantry.Pro.EfCore.MySql` provisions databases with MySqlConnector
and works with either provider. Use the combinations that are tested (below):

- **EF Core 8 or 9:** Pomelo, `options.UseMySql(cs, ServerVersion.AutoDetect(cs))`.
- **EF Core 10 (.NET 10):** Oracle's `MySql.EntityFrameworkCore`, `options.UseMySQL(cs)`. Pomelo has no
  EF Core 10 release.

Oracle's provider on EF Core 8 or 9, and either provider on EF Core 11 (.NET 11), are not yet tested. The
[MySQL sample](../samples/DatabasePerTenant.MySql) runs on .NET 10 with Oracle's provider.

## Tested combinations

The integration suites run migration orchestration against a real database in a container, on each
target framework: provisioning and migrating several tenant databases, a rerun that applies nothing, a
tenant that fails (rejected credentials) while the others migrate and is then repaired and retried, and
two runners migrating the same tenants at once. Recorded 29 September 2026.

| Database | EF Core provider | .NET / EF Core | Result | Concurrent runners |
|----------|------------------|----------------|--------|--------------------|
| SQL Server 2022 | `Microsoft.EntityFrameworkCore.SqlServer` 8.0.31 / 9.0.20 / 10.0.12 | 8, 9, 10 | Passing | Serialised on EF Core 9 and 10; race on EF Core 8 |
| PostgreSQL 16 | `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 / 9.0.0 / 10.0.3 | 8, 9, 10 | Passing | Serialised on EF Core 9 and 10; race on EF Core 8 |
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

## Writing a custom provider

Provisioning is a template method: `DatabaseProvisioningServiceBase<TKey>` and
`SchemaProvisioningServiceBase<TKey>` (in `Tenantry.Pro.EfCore`) implement the shared pipeline —
licence guard, tenant lookup, connection-string/schema resolution, logging — and defer only the
engine-specific `CREATE` statement and connection-string parsing to a subclass. To support another
relational engine, subclass the relevant base and register it as an
`ITenantInfrastructureProvisioner<TKey>`, mirroring the existing provider packages.

## See also

- [Database per tenant](database-per-tenant.md) · [Schema per tenant](schema-per-tenant.md)
- [Migration orchestration](migration-orchestration.md) · [Health checks](health-checks.md)
