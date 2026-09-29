# `LicenseOptions` class

Namespace: `Tenantry.Pro.Licensing` · Package: `Tenantry.Pro` · [API reference](README.md)

Options for configuring the Tenantry.Pro licence.

```csharp
public sealed class LicenseOptions
```

## Properties

### `LicenceKey`

The signed JWT licence key issued by Tenantry. Set via `tenant.UsePro(pro => pro.WithLicence(key))` in `Program.cs`.

```csharp
public string LicenceKey { get; set; }
```

Value: `string`
