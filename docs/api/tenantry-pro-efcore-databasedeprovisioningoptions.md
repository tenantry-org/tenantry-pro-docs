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

Creates the context the database is dropped through, from the tenant's scope, where the tenant is current. Use it to connect with credentials allowed to drop databases, when the application's own are not.

```csharp
public Func<IServiceProvider, TContext>? CreateContext { get; set; }
```

Value: `Func<IServiceProvider, TContext>`

It also creates each other tenant's context, to check none of them uses the database. The step disposes the context. When not set, the step uses the application's `TContext` from the tenant's scope, as a request gets it. When that is not registered, or has no connection string yet (Tenantry Core reads an asynchronous one when the context first connects), it uses the application's `IDbContextFactory<TContext>`.

### `IndependentMySqlServers`

Whether the MySQL servers the tenants' databases are on hold separate data: none is a replica of another, directly or through a chain, and none is in a replication group with another. By default [false](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool), and a drop is refused when another tenant has a database of the same name on a MySQL server with another `server_uuid`.

```csharp
public bool IndependentMySqlServers { get; set; }
```

Value: `bool`

A MySQL replica or group member has a `server_uuid` of its own, and `DROP DATABASE` on one server reaches every server that replicates it. Tenantry cannot read every such chain (A to B to C with GTIDs off), so it does not try. Set this to [true](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/bool) only when no server the tenants reach replicates another: otherwise a tenant that reaches its database through another server loses its data with the leaving tenant's. It has no effect on SQL Server or PostgreSQL, whose replicas share their primary's id.
