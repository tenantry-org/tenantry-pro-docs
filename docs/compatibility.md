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

**Legacy**: the net8.0 and net9.0 builds still ship and are tested, but Microsoft stops patching .NET 8 and 9 (and
EF Core 8 and 9) on 10 November 2026, so move to .NET 10. Tenantry.Pro 1.0, released no earlier than 10 November
2027, drops them.

## Tenantry Core

In the beta, Tenantry.Pro releases each minor version with Tenantry Core's, and runs on that Core minor:
Tenantry.Pro 0.5 on Tenantry Core from the version it is tested against up to, but not including, 0.6.0. Before 1.0
a Core minor release may break Pro, so a new Core minor comes with a new Tenantry.Pro minor. See
[Tenantry Core's compatibility](https://github.com/tenantry-org/tenantry-core/blob/master/docs/compatibility.md).

`Tenantry.Pro.EfCore` uses `Tenantry.Pro`'s internals, so it depends on exactly its own release of it.
`Tenantry.Pro.AspNetCore` and the Hangfire, MassTransit, Quartz.NET and Rebus packages use only its public API, so
each takes `Tenantry.Pro` from its own release up to the next minor. Update the packages together.

## EF Core and databases

Each target framework's build is compiled against that framework's EF Core major, and accepts any later
release of it: EF Core **8.0.31** or later 8.x on net8.0, **9.0.20** or later 9.x on net9.0, **10.0.12** or
later 10.x on net10.0. Use the EF Core provider that matches your target framework.

`Tenantry.Pro.EfCore` provisions through the EF Core provider your context uses, so it needs no provider
package or database driver of its own.

| Database | EF Core provider |
|----------|------------------|
| SQL Server | `Microsoft.EntityFrameworkCore.SqlServer` |
| PostgreSQL | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| MySQL / MariaDB | Pomelo on EF Core 8 and 9; Oracle's `MySql.EntityFrameworkCore` on EF Core 10 |

The combinations the integration suites run against a real database are in
[Database providers](database-providers.md#tested-combinations).

## Background jobs and messaging

| Package | Library | Versions |
|---------|---------|----------|
| `Tenantry.Pro.Hangfire` | Hangfire (`Hangfire.Core`, with no ASP.NET Core dependency) | 1.8 or later 1.x |
| `Tenantry.Pro.MassTransit` | MassTransit | 8.1 or later 8.x (MassTransit 9 is not supported) |
| `Tenantry.Pro.Quartz` | Quartz.NET | 3.8 or later 3.x |
| `Tenantry.Pro.Rebus` | Rebus | 8.4 or later 8.x |

Each package is compiled against one major version of its library, so it supports one major at a time:

- **A new major** (Quartz.NET 4, Hangfire 2 or Rebus 9, when they ship) replaces the previous one in a later
  Tenantry.Pro minor release, which the changelog announces. An application that stays on the previous major stays
  on the Tenantry.Pro minor before it.
- **MassTransit**: `Tenantry.Pro.MassTransit` stays on MassTransit 8, the open-source line, also after MassTransit
  stops patching it (its security and critical fixes are planned until at least the end of 2026). MassTransit 9 is
  sold under a commercial licence; support for it would come as a separate package, with an ID of its own, if
  customers need it.

## Native AOT and trimming

`Tenantry.Pro` and `Tenantry.Pro.AspNetCore` are trim- and Native AOT-compatible; the EF Core packages are
not, because EF Core is not. See [Troubleshooting](troubleshooting.md#trimaot-analyzer-warnings-il2026-il3050).

## Dependency versions

- **`Microsoft.Extensions.*`**: a minimum only, from the target framework's own major, with no upper bound.
  Current Azure SDKs need `Microsoft.Extensions` 10.x even on .NET 8.
- **EF Core**: the target framework's major only.
- **Hangfire, MassTransit, Quartz.NET and Rebus**: one major version, because their majors change the APIs these
  packages are compiled against.

CI checks every minimum is a version the tests run against, and a weekly job runs the whole test suite with
every dependency at the newest version it allows.
