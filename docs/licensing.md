# Licensing

Tenantry.Pro needs a licence key, which you copy from your [Pro access page](https://tenantry.dev/dashboard/pro)
and configure once. The key is validated **entirely offline** and **does not expire**: you set it when you
start using Pro and never have to change it. What your subscription controls is access to the private
package feed, and with it new versions of Pro; a key you were issued keeps working with the versions you
already have.

## How it works

- A licence key is an **ES256 (ECDSA P-256) JWT** issued by Tenantry. It names the key it was signed with
  (`kid`), and carries the issuer (`iss`), the product (`aud`), the licence format (`ver`) and the customer it
  was issued to (`sub`).
- Its signature is verified against an **ECDSA public key embedded in the `Tenantry.Pro` package**, the one
  its `kid` names. The private key never leaves Tenantry, and no network call is ever made. If Tenantry ever
  signs with a new key, a new version of Pro embeds it alongside the old one, so keys already issued keep
  working.
- `UsePro` reads the key from the `Tenantry:License` setting of your application's configuration, when the
  application starts:

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro());   // the licence key comes from Tenantry:License
```

Keep the key out of source control. Set it in user secrets, an environment variable (`Tenantry__License`), or
a secrets vault: anywhere your configuration can surface it as `Tenantry:License`. Your CI needs it too if it
runs the application or its integration tests; [Installation](installation.md#ci) shows both. When the key
comes from somewhere your configuration cannot read, pass it in code instead:

```csharp
tenant.UsePro(pro => pro.UseLicenseKey(licenseKey));
```

## When the key is checked

**At startup.** `UsePro` registers a hosted service that checks the key when the host starts. If no key is
configured, or the key is not valid (malformed, truncated, a bad signature, or not a Tenantry.Pro licence),
it throws `LicenseRequiredException` and **the application does not start**. The exception says whether the
key is missing or invalid, and the log says why an invalid key was rejected. A valid key logs one line naming
the customer it was issued to.

Because the check happens at startup and a valid key never expires, a licence problem shows up while you
develop or in CI, and can never stop an application that is already running.

**Before state-changing operations.** Pro also checks the key before the operations that change your
databases, so code that uses them outside a .NET host (where no hosted service runs) is covered too:

- tenant provisioning (`ITenantProvisioner.ProvisionAsync`), which creates databases and schemas
- applying migrations (`ITenantMigrationRunner.MigrateAllAsync` / `MigrateTenantAsync`, and at startup or as a
  deployment step)

These throw `LicenseRequiredException` for a missing or invalid key. The key is validated once and the
result kept, so the checks cost nothing after the first. Read-only operations are never checked: for
example `ITenantMigrationRunner.GetStatusAsync` reports applied and pending migrations without a licence.

## When your subscription ends

Your key keeps validating, so the versions of Pro you already have keep working. Which versions get security fixes is in
the [security policy](../.github/SECURITY.md#supported-versions). Today's GitHub feed stops restoring every version when
access ends, so keep copies of the packages you build with
([Installation](installation.md#when-your-subscription-ends)). The private Tenantry feed that replaces it before Pro
goes on sale will let you restore the versions released while you subscribed. If you subscribe again, your Pro access
page shows a new key.

## Reference

| Setting or member | Package | Purpose |
|------|---------|---------|
| `Tenantry:License` (`Tenantry__License`) | `Tenantry.Pro` | The configuration key `UsePro` reads the licence key from |
| `pro.UseLicenseKey(key)` | `Tenantry.Pro` | Sets the licence key in code, in place of the setting |
| `LicenseRequiredException` | `Tenantry.Pro` | Thrown at startup, and by guarded operations, when the key is missing or invalid |

Tests that start your application need a licence key too, like CI: there is no way to switch the check off.

## See also

- [Installation](installation.md) — the private package feed, and setting the key locally and in CI.
- [Getting started](getting-started.md) — where `UsePro` fits in registration.
- [Troubleshooting](troubleshooting.md) — diagnosing `LicenseRequiredException`.
