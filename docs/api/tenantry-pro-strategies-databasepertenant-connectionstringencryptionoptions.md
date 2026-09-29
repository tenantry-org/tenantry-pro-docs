# `ConnectionStringEncryptionOptions` class

Namespace: `Tenantry.Pro.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Configures at-rest encryption for cached connection strings. Set via `opts.Encryption.Mode = ...` inside `pro.UseDatabasePerTenant(...)`.

```csharp
public sealed class ConnectionStringEncryptionOptions
```

## Properties

### `AesKey`

A 256-bit (32-byte) AES-256 symmetric key. Required when [`ConnectionStringEncryptionOptions.Mode`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionoptions.md) is [`ConnectionStringEncryptionMode.AesKey`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md). **Do not store this value in source control.** Load it from environment variables, Azure Key Vault, AWS Secrets Manager, or similar.

```csharp
public byte[]? AesKey { get; set; }
```

Value: `byte[]`

```csharp
opts.Encryption.AesKey = Convert.FromBase64String(config["Tenantry:EncryptionKey"]!);
```

### `Mode`

The encryption mode. Default: [`ConnectionStringEncryptionMode.None`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md) (no encryption).

```csharp
public ConnectionStringEncryptionMode Mode { get; set; }
```

Value: [`ConnectionStringEncryptionMode`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionmode.md)
