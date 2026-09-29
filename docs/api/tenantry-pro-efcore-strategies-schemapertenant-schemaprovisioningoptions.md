# `SchemaProvisioningOptions` class

Namespace: `Tenantry.Pro.EfCore.Strategies.SchemaPerTenant` · Package: `Tenantry.Pro.EfCore` · [API reference](README.md)

Options for schema-per-tenant provisioning. Configure via `pro.AddSchemaProvisioning(opts => opts.ConnectionString = ...)`.

```csharp
public sealed class SchemaProvisioningOptions
```

## Properties

### `ConnectionString`

The ADO.NET connection string for the shared database in which schemas are provisioned.

```csharp
public string? ConnectionString { get; set; }
```

Value: `string`
