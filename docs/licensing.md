# Licensing

Tenantry.Pro needs a licence key, which you copy from your [Pro access page](https://tenantry.dev/dashboard/pro)
and configure once. The key is validated **entirely offline** and **does not expire**: you set it when you
start using Pro and never have to change it. What your subscription controls is access to the private
package feed, and with it new versions of Pro; a key you were issued keeps working with the versions you
already have.

## How it works

- A licence key is an **ES256 (ECDSA P-256) JWT** issued by Tenantry. It carries the issuer (`iss`) and the
  customer it was issued to (`sub`).
- Its signature is verified against an **ECDSA public key embedded in the `Tenantry.Pro` package**. The
  private key never leaves Tenantry, and no network call is ever made.
- You configure the key once, in the `UsePro` lambda:

```csharp
tenant.UsePro(pro =>
{
    pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
    // …strategies and features…
});
```

Keep the key out of source control. Bind it from user secrets, an environment variable, or a secrets
vault — anywhere your configuration provider can surface it as `Tenantry:Licence`. Your CI needs it too if
it runs the application or its integration tests; [Installation](installation.md#ci) shows both.

## When the key is checked

**At startup.** `UsePro` registers a hosted service that checks the key when the host starts. If no key is
configured, or the key is not valid (malformed, truncated, a bad signature or another issuer), it throws
`LicenseRequiredException` and **the application does not start**. The exception says whether the key is
missing or invalid, and the log says why an invalid key was rejected. A valid key logs one line naming the
customer it was issued to.

Because the check happens at startup and a valid key never expires, a licence problem shows up while you
develop or in CI, and can never stop an application that is already running.

**Before state-changing operations.** Pro also checks the key before the operations that change your
databases, so code that uses them outside a .NET host (where no hosted service runs) is covered too:

- database and schema provisioning (`DatabaseProvisioningService` / `SchemaProvisioningService`)
- migration orchestration (`MigrationOrchestratorService.MigrateAllAsync` / `MigrateTenantAsync`)
- the tenant lifecycle pipeline (`ITenantLifecycleManager.ProvisionAsync`)

These throw `LicenseRequiredException` for a missing or invalid key. The key is validated once and the
result kept, so the checks cost nothing after the first. Read-only operations are never checked: for
example `MigrationStatusTracker` reports applied and pending migrations without a licence.

## When your subscription ends

Your key keeps validating, so the versions of Pro you already have keep working. You lose access to the
private package feed, so you cannot restore or update to versions of Pro released afterwards. If you
subscribe again, your Pro access page shows a new key.

## Reference

| Type | Package | Purpose |
|------|---------|---------|
| `pro.WithLicence(key)` | `Tenantry.Pro` | Configure the licence key |
| `LicenseRequiredException` | `Tenantry.Pro` | Thrown at startup, and by guarded operations, when the key is missing or invalid |
| `ILicenseGuard` | `Tenantry.Pro` | The check guarded operations run; replace it only in tests |

## See also

- [Installation](installation.md) — the private package feed, and setting the key locally and in CI.
- [Getting started](getting-started.md) — where `WithLicence` fits in registration.
- [Troubleshooting](troubleshooting.md) — diagnosing `LicenseRequiredException`.
