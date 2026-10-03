# Tenantry.Pro samples

Runnable samples for Tenantry.Pro 0.5.0 (on Tenantry Core 0.5.0), referenced from the
[documentation](https://tenantry.dev/docs/pro). They restore Tenantry.Pro from the private package feed, so
set up your credentials first: see [Installation](https://tenantry.dev/docs/pro/installation).

Each sample reads your licence key from the configuration key `Tenantry:License`. Set it once for your
shell as an environment variable, then run any sample from this directory:

```bash
export Tenantry__License="<your-licence-key>"   # PowerShell: $env:Tenantry__License = "<your-licence-key>"
dotnet run --project Tenantry.Pro.Samples.AuditLogging
```

or pass it to a single run:
`dotnet run --project Tenantry.Pro.Samples.AuditLogging --Tenantry:License "<your-licence-key>"`. The samples
contain no licence key or token: keep yours in environment variables, never in the files you commit.
