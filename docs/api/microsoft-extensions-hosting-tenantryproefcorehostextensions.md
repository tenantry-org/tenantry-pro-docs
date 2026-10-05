# `TenantryProEfCoreHostExtensions` class

Namespace: `Microsoft.Extensions.Hosting` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Runs Tenantry.Pro's tenant migrations as a deployment step.

```csharp
public static class TenantryProEfCoreHostExtensions
```

## Fields

### `MigrateTenantsArgument`

The command-line argument that asks for the migrations to be run: `migrate-tenants`.

```csharp
public const string MigrateTenantsArgument = "migrate-tenants"
```

Returns: `string`

It has no leading dashes, because ASP.NET Core's command-line configuration would read `--migrate-tenants` as a setting and take the next argument as its value.

## Methods

### `RunTenantMigrationsIfRequestedAsync(IHost, string[], CancellationToken)`

When `args` include `migrate-tenants`, applies the migrations of every context added with `pro.AddMigrations<TContext>()`, for the tenants the arguments select (every tenant by default), and returns the exit code for the process. Otherwise returns [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) at once.

```csharp
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
public static Task<int?> RunTenantMigrationsIfRequestedAsync(this IHost host, string[] args, CancellationToken cancellationToken = default)
```

Parameters:

- `host` `IHost`: The built application, not yet running.
- `args` `string[]`: The application's command-line arguments. With `migrate-tenants`, they may hold `--tenant <id>` (repeatable: only those tenants), `--exclude <id>` (repeatable: leave them out) and `--max-failures <n>` (stop starting databases once n have failed), as `--name value` or `--name=value`; see [`MigrationRunOptions<TKey>`](tenantry-pro-efcore-migrationrunoptions.md). Other arguments are left alone, for the application's configuration.
- `cancellationToken` `CancellationToken`: Abandons the migrations in progress and starts no other. A first SIGTERM or Ctrl+C during the run instead lets those in progress finish and starts no other; a second ends the process.

Returns: `Task<int?>`: The exit code, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the arguments did not ask for the migrations: 0 when every selected database and schema is migrated; 1 when any failed; 2 when the run stopped before attempting them all (`--max-failures`, or a stop signal); 3 when it could not start (invalid arguments, no valid licence, a tenant the store does not have, a tenant store that cannot be read, or schema per tenant with more than one context type that uses `UseTenantry()` and none listed in `SchemaPerTenantOptions.Contexts`). Each failure is logged.

Exceptions:

- `InvalidOperationException`: `pro.AddMigrations<TContext>()` was not called.

Run the application with `migrate-tenants` once per deployment, before the new version serves requests (a Kubernetes job, a release pipeline step), and let the process end: it does not start the host.

```csharp
var app = builder.Build();
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;
```
