# Compatibility

Which .NET versions, EF Core versions, databases and libraries Tenantry.Pro supports, and how its package
dependencies are declared.

## .NET versions

| .NET | Microsoft support | Tenantry.Pro | Supported until |
|------|-------------------|--------------|-----------------|
| **.NET 10** | LTS, until 14 November 2028 | **Primary** | .NET 10's end of support |
| **.NET 11** | STS, two years from its release in November 2026 | **Primary once .NET 11 ships** (built and tested against the release candidate until then) | .NET 11's end of support |
| .NET 9 | STS, until 10 November 2026 | Legacy | 10 November 2027, one year after Microsoft's end of support |
| .NET 8 | LTS, until 10 November 2026 | Legacy | 10 November 2027, one year after Microsoft's end of support |

The packages target **net8.0, net9.0 and net10.0**; net11.0 is added when .NET 11 is released.

**Legacy** means the net8.0 and net9.0 builds are still shipped, built and tested, but Microsoft stops
patching .NET 8 and .NET 9, including EF Core 8 and 9, on 10 November 2026. Move to .NET 10.
They stay through the beta: 1.0 ends it, not before 10 November 2027, and drops them.

## Tenantry Core

Tenantry.Pro runs on Tenantry Core from the version it is tested against up to, but not including, 1.0.0.
The Tenantry.Pro packages depend on each other at exactly the same version: update them together. See
[Tenantry Core's compatibility](https://github.com/tenantry-org/tenantry-core/blob/master/docs/compatibility.md).

## EF Core and databases

Each target framework's build is compiled against that framework's EF Core major, and accepts any later
release of it: EF Core **8.0.31** or later 8.x on net8.0, **9.0.20** or later 9.x on net9.0, **10.0.12** or
later 10.x on net10.0. Use the EF Core provider that matches your target framework.

| Database | EF Core provider | Provisioning package | Driver it needs |
|----------|------------------|----------------------|-----------------|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` | `Tenantry.Pro.EfCore.SqlServer` | `Microsoft.Data.SqlClient` 5.2.0 or later (6.1.1 or later on net10.0) |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` | `Tenantry.Pro.EfCore.Npgsql` | `Npgsql` of the framework's major: 8.0.3+ 8.x, 9.x, 10.x |
| MySQL / MariaDB | Pomelo on EF Core 8 and 9; Oracle's `MySql.EntityFrameworkCore` on EF Core 10 | `Tenantry.Pro.EfCore.MySql` | `MySqlConnector` 2.3.5 or later |

The combinations the integration suites run against a real database are in
[Database providers](database-providers.md#tested-combinations).

## Background jobs and messaging

| Package | Library | Versions |
|---------|---------|----------|
| `Tenantry.Pro.Hangfire` | Hangfire | 1.8 or later 1.x |
| `Tenantry.Pro.MassTransit` | MassTransit | 8.x (MassTransit 9 is not supported) |
| `Tenantry.Pro.Quartz` | Quartz.NET | 3.8 or later 3.x |
| `Tenantry.Pro.Rebus` | Rebus | 8.x |

## Native AOT and trimming

`Tenantry.Pro` and `Tenantry.Pro.AspNetCore` are trim- and Native AOT-compatible; the EF Core packages are
not, because EF Core is not. See [Troubleshooting](troubleshooting.md#trimaot-analyzer-warnings-il2026-il3050).

## Dependency versions

- **`Microsoft.Extensions.*`, `Microsoft.Data.SqlClient`, `MySqlConnector`, `Azure.Identity` and
  `Microsoft.Identity.Client`**: a minimum only, with no upper bound (`Microsoft.Extensions.*` from the
  target framework's own major). Current Azure SDKs need `Microsoft.Extensions` 10.x even on .NET 8, and the
  EF Core providers do not cap their drivers either.
- **EF Core and Npgsql**: the target framework's major only.
- **Hangfire, MassTransit, Quartz.NET, Rebus and Newtonsoft.Json**: one major version, because their majors
  change the APIs these packages are compiled against.

CI checks every minimum is a version the tests run against, and a weekly job runs the whole test suite with
every dependency at the newest version it allows.
