# `ILicenseGuard` interface

Namespace: `Tenantry.Pro.Licensing` · Package: `Tenantry.Pro` · [API reference](README.md)

Checks that a valid Tenantry.Pro licence is configured. A public seam so that public, provider-agnostic services (such as the EF Core provisioning services) can take the licence check in a `public` constructor, while the validator itself stays internal.

```csharp
public interface ILicenseGuard
```

## Methods

### `EnsureLicensed(string)`

Throws unless the configured licence key is valid: present, signed by Tenantry and well formed. The result is computed once, since the key does not change while the application runs.

```csharp
void EnsureLicensed(string capability)
```

Parameters:

- `capability` `string`: A short phrase naming the operation being guarded (e.g. "provision tenant databases"), used in the exception message.

Exceptions:

- [`LicenseRequiredException`](tenantry-pro-exceptions-licenserequiredexception.md): The licence key is missing or invalid.
