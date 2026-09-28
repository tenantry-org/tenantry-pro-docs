# Licensing

Tenantry.Pro ships a signed licence key that validates **entirely offline**. The licence is a
**provenance and identity signal, not a kill switch**: the real commercial gate is access to the
private package feed, so by default a licence problem is **logged but never interrupts your
application**. Hard enforcement is available as an explicit opt-in for teams that want a fail-fast
check in their own pipeline.

> **Why non-fatal by default?** A paying customer's production app must never go down because of *our*
> licence logic — clock skew, a mistyped key, or a grace-edge bug should never cause an outage. See
> `LicenseEnforcement` for the two modes.

## How it works

- A licence key is an **ES256 (ECDSA P-256) JWT** issued by Tenantry. It carries an issuer (`iss`)
  and an expiry (`exp`).
- The key's signature is verified against an **ECDSA public key embedded in the `Tenantry.Pro`
  package**. The corresponding private key never leaves Tenantry, and no network call is ever made.
- You configure the key once, in the `UsePro` lambda:

```csharp
tenant.UsePro(pro =>
{
    pro.WithLicence(builder.Configuration["Tenantry:Licence"]!);
    // …strategies and features…
});
```

Keep the key out of source control. Bind it from user secrets, an environment variable, or a secrets
vault — anywhere your configuration provider can surface it as `Tenantry:Licence`.

## Enforcement modes

`WithLicence` takes an optional `LicenseEnforcement` argument that controls how the runtime reacts to
a missing, invalid, or grace-expired licence:

```csharp
using Tenantry.Pro.Licensing;

// Default — non-fatal. Licence problems are logged; the app keeps running.
pro.WithLicence(key);
pro.WithLicence(key, LicenseEnforcement.Warn);

// Opt-in — fail-fast. Guarded operations throw and UseLicenseCheck() returns 503.
pro.WithLicence(key, LicenseEnforcement.Throw);
```

| | `Warn` (default) | `Throw` (opt-in) |
|--|------------------|------------------|
| Missing / invalid / wrong issuer / bad signature | logged as an error; operation continues | `LicenseRequiredException`; middleware returns 503 |
| Expired, grace period over | logged as an error; operation continues | `LicenseRequiredException`; middleware returns 503 |
| Expired, within 30-day grace | logged as a warning; runs normally | logged as a warning; runs normally |
| Valid | runs normally | runs normally |

The 30-day grace period applies in **both** modes: a recently lapsed licence keeps working while you
renew. The difference is only what happens once a licence is missing/invalid or grace is over — `Warn`
keeps your app up, `Throw` stops the guarded operation.

## What is licence-guarded

Under `LicenseEnforcement.Throw`, the licence is checked before **state-changing Pro operations**:

- database and schema provisioning (`DatabaseProvisioningService` / `SchemaProvisioningService`)
- migration orchestration (`MigrationOrchestratorService.MigrateAllAsync` / `MigrateTenantAsync`)
- the tenant lifecycle pipeline (`ITenantLifecycleManager.ProvisionAsync`)

Read-only operations are **never** gated — for example `MigrationStatusTracker` reports
applied/pending migrations without requiring a licence, so monitoring keeps working regardless of
licence state.

Under the default `Warn` mode these same operations log a clear error naming the operation (e.g. *"no
valid licence is configured to provision tenant databases"*) and then proceed. Under `Throw` they
raise `LicenseRequiredException` (a subclass of `InvalidOperationException`) with the same message.

## Startup validation (all hosts)

Calling `UsePro` automatically registers a hosted service that logs the licence status once at
startup — valid, expiring within the grace period, or a problem (missing/invalid/grace-over). This
works in **any** host (web, worker, console) and is **always non-blocking regardless of enforcement
mode**: it never prevents the app from starting. Watch your logs at boot for a line beginning
`Tenantry.Pro:` to confirm the licence is healthy.

## Per-request enforcement (ASP.NET Core)

`Tenantry.Pro.AspNetCore` adds optional middleware that enforces the licence on **every HTTP
request**:

```csharp
using Tenantry.Pro.AspNetCore;

var app = builder.Build();

app.UseLicenseCheck();   // place early — before UseRouting / UseTenantry
app.UseTenantry();
```

Behaviour **depends on the enforcement mode** set via `WithLicence`:

- Under the default **`Warn`** mode the middleware logs licence problems but lets **every** request
  through — it never returns 503. The problem is logged on the first request and then at most once an
  hour, not on every request.
- Under **`Throw`** mode (`pro.WithLicence(key, LicenseEnforcement.Throw)`):
  - **No key / invalid signature / wrong issuer** → HTTP 503 with a plain-text body pointing at
    tenantry.dev.
  - **Expired but within grace** → request continues; a warning is logged.
  - **Expired, grace over** → HTTP 503.
  - **Valid** → request continues unchanged.

The middleware is a singleton and memoises the expensive ECDSA verification, so the signature is
parsed once; only the cheap date-based grace check is recomputed per request. That means a
long-running process in `Throw` mode correctly starts returning 503 the moment the clock crosses the
end of the grace period, without re-verifying the signature on every call.

Add `UseLicenseCheck()` only if you have opted into `LicenseEnforcement.Throw` and want hard
per-request enforcement. For most deployments the non-fatal default plus the startup log line is the
right choice — the private package feed is the real commercial gate.

## Reference

| Type | Package | Purpose |
|------|---------|---------|
| `pro.WithLicence(key, enforce)` | `Tenantry.Pro` | Configure the licence key and enforcement mode |
| `LicenseEnforcement` | `Tenantry.Pro` | `Warn` (default, non-fatal) or `Throw` (fail-fast) |
| `LicenseRequiredException` | `Tenantry.Pro` | Thrown by guarded operations under `Throw` when unlicensed |
| `app.UseLicenseCheck()` | `Tenantry.Pro.AspNetCore` | Per-request 503 enforcement (only acts under `Throw`) |

## See also

- [Getting started](getting-started.md) — where `WithLicence` fits in registration.
- [Troubleshooting](troubleshooting.md) — diagnosing 503s and `LicenseRequiredException`.
