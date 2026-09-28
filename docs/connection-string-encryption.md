# Connection-string encryption

When database-per-tenant connection strings are **cached**, Tenantry.Pro can encrypt the cached values
at rest so plaintext credentials never sit in memory in the connection-string cache. This matters most
when connection strings are resolved from a secrets vault (`GetConnectionStringAsync`) and cached to
avoid repeated lookups.

> **Encryption only applies to cached values.** It has no effect unless
> `CacheConnectionStrings = true`. `ITenantConnectionStringResolver<TKey>` always returns **plaintext** to
> callers — encryption and decryption are transparent and happen only at the cache boundary.

## Modes

Encryption is configured on `DatabasePerTenantOptions.Encryption`. There are three modes plus an
ASP.NET Core Data Protection option:

| Mode | How | Key management |
|------|-----|----------------|
| `None` (default) | Cache stores plaintext | — |
| `AesKey` | AES-256 with a key you supply | You provide and rotate a 32-byte key |
| `Custom` | Your own `IConnectionStringProtector` | Entirely yours |
| Data Protection | `pro.UseDataProtectionEncryption()` (ASP.NET Core) | Managed by ASP.NET Core Data Protection |

### AES-256 with your own key

Provide a 32-byte (256-bit) key. **Never** commit it — load it from a secrets store:

```csharp
using Tenantry.Pro.Strategies.DatabasePerTenant;

pro.UseDatabasePerTenant(opts =>
{
    opts.GetConnectionStringAsync = async (t, ct) => await vault.GetAsync($"cs-{t.TenantId}", ct);
    opts.CacheConnectionStrings = true;

    opts.Encryption.Mode = ConnectionStringEncryptionMode.AesKey;
    opts.Encryption.AesKey = Convert.FromBase64String(builder.Configuration["Tenantry:EncryptionKey"]!);
});
```

The key must be exactly 32 bytes or registration throws. You own rotation: when you change the key,
the existing cache entries can no longer be decrypted and are re-resolved.

### Custom protector

Implement `IConnectionStringProtector` and register it before `UseDatabasePerTenant`, then select
`Custom`:

```csharp
public sealed class MyKmsProtector : IConnectionStringProtector
{
    public string Protect(string connectionString)   => kms.Encrypt(connectionString);
    public string Unprotect(string protectedValue)    => kms.Decrypt(protectedValue);
}

builder.Services.AddSingleton<IConnectionStringProtector, MyKmsProtector>();

pro.UseDatabasePerTenant(opts =>
{
    opts.CacheConnectionStrings = true;
    opts.Encryption.Mode = ConnectionStringEncryptionMode.Custom;
});
```

### ASP.NET Core Data Protection

`Tenantry.Pro.AspNetCore` provides a protector backed by ASP.NET Core Data Protection, whose keys are
created, rotated, and persisted for you. Call `AddDataProtection()` first, then
`UseDataProtectionEncryption()` — note this is enabled by a **method call**, not by setting
`Encryption.Mode`:

```csharp
using Tenantry.Pro.AspNetCore;

builder.Services.AddDataProtection();   // required first

builder.Services.AddTenantry<string>(tenant =>
{
    tenant.UsePro(pro =>
    {
        pro.UseDataProtectionEncryption();   // registers the IConnectionStringProtector

        pro.UseDatabasePerTenant(opts =>
        {
            opts.GetConnectionStringAsync = async (t, ct) => await vault.GetAsync($"cs-{t.TenantId}", ct);
            opts.CacheConnectionStrings = true;   // protector only kicks in when caching is on
        });
    });
});
```

If `IDataProtectionProvider` is not registered, resolution throws with a message telling you to call
`AddDataProtection()`. If caching is off, the protector is never consulted and a warning is logged at
startup so the no-op is not silent.

## Choosing a mode

- **Vault-resolved connection strings + caching** → encrypt. Prefer **Data Protection** in ASP.NET
  Core (no key material to manage), or **AesKey** when you must control the key (e.g. a shared key
  across services).
- **Connection strings derived in-process from non-secret tenant properties** → caching may be
  unnecessary, and `None` is fine.

## See also

- [Database per tenant](database-per-tenant.md) — where caching is configured.
