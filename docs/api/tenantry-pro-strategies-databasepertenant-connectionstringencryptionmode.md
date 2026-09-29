# `ConnectionStringEncryptionMode` enum

Namespace: `Tenantry.Pro.Strategies.DatabasePerTenant` · Package: `Tenantry.Pro` · [API reference](README.md)

Determines how connection strings are encrypted before being stored in the cache.

```csharp
public enum ConnectionStringEncryptionMode
```

## Values

| Value | Description |
|-------|-------------|
| `None = 0` | No encryption. Connection strings are cached in plain text. This is the default — backward-compatible with all existing configurations. |
| `AesKey = 1` | Encrypt using a consumer-provided 256-bit (32-byte) AES-256-GCM key. Set [`ConnectionStringEncryptionOptions.AesKey`](tenantry-pro-strategies-databasepertenant-connectionstringencryptionoptions.md) before calling `UseDatabasePerTenant`. The consumer is responsible for key management and rotation. |
| `Custom = 2` | Use a consumer-provided [`IConnectionStringProtector`](tenantry-pro-strategies-databasepertenant-iconnectionstringprotector.md) implementation. Register the implementation in DI before calling `UseDatabasePerTenant`. |
