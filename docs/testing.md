# Testing

Test a Tenantry.Pro application as
[Tenantry core's testing guide](https://github.com/tenantry-org/tenantry-core/blob/master/docs/testing.md)
describes: with Tenantry's real services, an in-memory tenant store, and each tenant's scope. This page covers what
Pro adds: the licence key, tenant databases and schemas, and jobs and messages.

## The licence key

Tenantry.Pro checks the licence key when the application starts, and before provisioning and migrations
([When the key is checked](licensing.md#when-the-key-is-checked)), in tests too: there is no way to switch the check
off. So a test that starts the application, or provisions or migrates a tenant, needs the key.

- A test host (`WebApplicationFactory`, or a host the test builds) reads `Tenantry:License` from its configuration,
  as the application does. Set the `Tenantry__License` environment variable wherever the tests run: from a secret in
  CI ([Installation](installation.md#ci)), and in your shell or your IDE's test settings locally.
  `WebApplicationFactory` runs the application in the `Development` environment, so your application's user secrets
  work too.
- A service provider the test builds itself, with no host, has no configuration: pass the key with
  `pro.UseLicenseKey(key)`, read from the same environment variable.

Code that does not provision or migrate needs no key when you test it on its own: a seeder, a provisioning step, a
job or a message handler, run in a tenant's scope without `UsePro`.

## Tenant databases and schemas

Provisioning, migrations and health checks run SQL that your database interprets, so test them against the database
you deploy on, in a container ([Testcontainers](https://dotnet.testcontainers.org/), for example). Register Tenantry
as the application does (an extension method that `Program.cs` and the tests both call keeps them the same), with a
tenant of the test's own, then provision it and look at what it created:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tenantry;
using Tenantry.Pro;
using Xunit;

public class ProvisioningTests
{
    // A SQL Server for the tests, such as a container's; each test makes its own tenant, so its own database.
    private static readonly string Server = Environment.GetEnvironmentVariable("TEST_SQL_SERVER")
        ?? throw new InvalidOperationException("Set TEST_SQL_SERVER to a SQL Server connection string.");

    [Fact]
    public async Task ANewTenantGetsAMigratedDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        var tenant = new TenantDescriptor<string> { TenantId = $"test{Guid.NewGuid():N}", Name = "Test" };

        // A host, as the application builds one: UsePro reads the licence key from Tenantry__License.
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddTenantry<string>(t =>
        {
            t.UseInMemoryStore([tenant]);
            t.UseConnectionStrings(o => o.GetConnectionString = d => $"{Server};Database=app_{d.TenantId}");
            t.UsePro(pro => pro
                .AddDatabaseProvisioning<AppDbContext>()
                .AddMigrations<AppDbContext>());
            t.AddDbContextPerTenantDatabase<AppDbContext>((_, options) => options.UseSqlServer());
        });
        using var host = builder.Build();

        var result = await host.Services.GetRequiredService<ITenantProvisioner<string>>().ProvisionAsync(tenant, ct);
        Assert.True(result.Succeeded);

        await using var scope = host.Services.GetRequiredService<ITenantScopeFactory<string>>().CreateScope(tenant);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(ct));
    }
}
```

The host is built but not started: provisioning and migrations check the key themselves. Start it
(`await host.StartAsync(ct)`) to run the application's hosted services too, the startup checks among them.

## Jobs and messages

Test a job or a message handler like any code that reads the tenant: resolve it from a tenant's scope and run it.

To test the wiring, start the application. An integration that is registered but not wired into its library (a
Quartz.NET scheduler without `q.UseTenantry()`, a MassTransit or Rebus bus without `UseTenantry`, Hangfire without
`config.UseTenantry(sp)`) stops the application from starting, so a test that starts it, with
`WebApplicationFactory` or `host.StartAsync()`, catches a missing call. To check that a job or a message runs as its
tenant, run the application with the library's in-memory storage or transport, as the
[samples](../samples) do, and check what the job or handler saw.

## See also

- [Licensing](licensing.md): when the key is checked.
- [Installation](installation.md#ci): the key in CI.
- [Tenant lifecycle](tenant-lifecycle.md): what provisioning runs.
- [Background jobs & non-HTTP hosts](background-jobs.md): tenant scopes outside a request.
