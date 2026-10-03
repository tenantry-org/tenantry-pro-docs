# `LicenseRequiredException` class

Namespace: `Tenantry.Pro` · Package: `Tenantry.Pro` · [API reference](README.md)

Thrown when no Tenantry.Pro licence key is configured, or the configured key is invalid (malformed, a bad signature, or not a Tenantry.Pro licence): when the application starts, and by licence-guarded operations.

```csharp
public sealed class LicenseRequiredException : InvalidOperationException, ISerializable
```

Inherits `Exception` → `SystemException` → `InvalidOperationException`.

Implements `ISerializable`.

## Constructors

### `LicenseRequiredException(string)`

Initialises a new instance of [`LicenseRequiredException`](tenantry-pro-licenserequiredexception.md).

```csharp
public LicenseRequiredException(string message)
```

Parameters:

- `message` `string`: The message that describes the licence problem.
