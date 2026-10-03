# `TenantryProEfCoreHostExtensions` class

Namespace: `Microsoft.Extensions.Hosting` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Runs Tenantry.Pro's tenant migrations as a deployment step.

```csharp
public static class TenantryProEfCoreHostExtensions
```

## Fields

### `MigrateTenantsArgument`

The command-line argument that asks for the migrations to be run: `migrate-tenants`. It has no leading dashes, because ASP.NET Core's command-line configuration would read `--migrate-tenants` as a setting and take the next argument as its value.

```csharp
public const string MigrateTenantsArgument = "migrate-tenants"
```

Returns: `string`

## Methods

### `RunTenantMigrationsIfRequestedAsync(IHost, string[], CancellationToken)`

When `args` include `migrate-tenants`, applies the migrations of every context added with `pro.AddMigrations<TContext>()`, for every tenant, and returns the exit code for the process: 0, or 1 if any database or schema failed (each failure is logged). Otherwise returns [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) at once.

```csharp
[RequiresUnreferencedCode("EF Core migrations use reflection and are not trim-safe. Use a migration bundle for trimmed applications.")]
[RequiresDynamicCode("EF Core migrations use dynamic code generation and are not Native AOT-compatible. Use a migration bundle for AOT applications.")]
public static Task<int?> RunTenantMigrationsIfRequestedAsync(this IHost host, string[] args, CancellationToken cancellationToken = default)
```

Parameters:

- `host` `IHost`: The built application, not yet running.
- `args` `string[]`: The application's command-line arguments.
- `cancellationToken` `CancellationToken`: Stops the run before the next database or schema.

Returns: `Task<int?>`: The exit code, or [null](https://learn.microsoft.com/dotnet/csharp/language-reference/keywords/null) when the arguments did not ask for the migrations.

Exceptions:

- `InvalidOperationException`: `pro.AddMigrations<TContext>()` was not called.
- [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md): The licence key is missing or invalid; nothing was migrated.

Run the application with `migrate-tenants` once per deployment, before the new version serves requests (a Kubernetes job, a release pipeline step), and let the process end: it does not start the host.

```csharp
var app = builder.Build();
if (await app.RunTenantMigrationsIfRequestedAsync(args) is { } exitCode)
    return exitCode;
```
