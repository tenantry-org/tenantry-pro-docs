# Installation

Tenantry.Pro's packages are published to a **private GitHub Packages feed** owned by the `tenantry-org`
GitHub organisation. Tenantry core (`Tenantry.Core`, `Tenantry.AspNetCore`, `Tenantry.EfCore`) and
everything else stay on nuget.org. This page sets up a machine and a CI pipeline to restore from both.

A private Tenantry feed will replace GitHub Packages before Tenantry.Pro goes on sale. After a subscription ends, it
will still let you restore the versions released while you subscribed, security patches included.

## What you need

1. **A Tenantry Pro subscription**, and your GitHub account connected on your
   [Pro access page](https://tenantry.dev/dashboard/pro). Connecting it invites that account to the
   `tenantry-org` organisation; **accept the invitation** (GitHub emails it, and the Pro access page links
   to it). Membership of the organisation is what gives read access to the feed.
2. **A GitHub personal access token (classic)** created by that same account, with only the
   **`read:packages`** scope. GitHub Packages accepts classic tokens only: fine-grained tokens do not work
   with the NuGet registry. [Create one](https://github.com/settings/tokens/new?scopes=read:packages&description=Tenantry%20Pro%20packages)
   and give it an expiry date.
3. **Your licence key**, from the same Pro access page. It does not expire; see [Licensing](licensing.md).

## 1. Add a `nuget.config`

Put this `nuget.config` next to your solution file and commit it. It holds no secrets: the credentials are
read from two environment variables.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="tenantry-pro" value="https://nuget.pkg.github.com/tenantry-org/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="tenantry-pro">
      <package pattern="Tenantry.Pro" />
      <package pattern="Tenantry.Pro.*" />
    </packageSource>
  </packageSourceMapping>
  <packageSourceCredentials>
    <tenantry-pro>
      <add key="Username" value="%TENANTRY_GITHUB_USERNAME%" />
      <add key="ClearTextPassword" value="%TENANTRY_GITHUB_PAT%" />
    </tenantry-pro>
  </packageSourceCredentials>
</configuration>
```

What each part does:

- **`<clear />`** drops sources inherited from your user or machine configuration, so every machine and CI
  runner restores from exactly these two.
- **`packageSourceMapping`** sends `Tenantry.Pro` and `Tenantry.Pro.*` to the private feed and everything
  else to nuget.org. The most specific pattern wins, so `Tenantry.Core` and the rest of Tenantry core come
  from nuget.org, and your other dependencies are never requested from GitHub. It also means no other
  source can supply a package named `Tenantry.Pro`.
- **`packageSourceCredentials`** authenticates to the private feed. `%NAME%` is replaced with the
  environment variable's value (on Windows, macOS and Linux). The element name must match the source's
  key, `tenantry-pro`.

If your solution already has a `nuget.config`, merge these entries into it. With source mapping on, every
source needs at least one pattern, so map any other feeds you use (a company feed, say) as well.

## 2. Set the credentials on your machine

Set the two variables in your shell profile or user environment:

```bash
# macOS / Linux
export TENANTRY_GITHUB_USERNAME=your-github-username
export TENANTRY_GITHUB_PAT=ghp_your_token
```

```powershell
# Windows
[Environment]::SetEnvironmentVariable('TENANTRY_GITHUB_USERNAME', 'your-github-username', 'User')
[Environment]::SetEnvironmentVariable('TENANTRY_GITHUB_PAT', 'ghp_your_token', 'User')
```

Restart your terminal and IDE so they pick the variables up.

## 3. Add the packages

```bash
dotnet add package Tenantry.AspNetCore            # Tenantry core, from nuget.org
dotnet add package Tenantry.Pro                    # from the private feed
dotnet add package Tenantry.Pro.EfCore             # from the private feed
```

[Getting started](getting-started.md) lists which packages each setup needs.

## 4. Configure the licence key

`UsePro` reads the key from the configuration key `Tenantry:License` when the application starts, so it
needs no code:

```csharp
builder.Services.AddTenantry<string>(tenant => tenant
    .ResolveFromHeader("X-Tenant-Id")
    .UseStore<MyTenantStore>()
    .UsePro());   // the licence key comes from Tenantry:License
```

If the key comes from somewhere your configuration cannot read, pass it with `pro.UseLicenseKey(key)` instead.

Keep the key out of source control. Locally, use user secrets, from your application's project directory
(`init` adds the project's `UserSecretsId`, once):

```bash
dotnet user-secrets init
dotnet user-secrets set "Tenantry:License" "<your licence key>"
```

User secrets are read only when the application runs in the `Development` environment, as `dotnet run`
does with the launch profile of a new web project; elsewhere, use the environment variable.

Anywhere else, set the environment variable `Tenantry__License` (two underscores stand for the `:`), or
use your secrets vault's configuration provider. Without a valid key the application does not start, so a
missing key shows up straight away.

## CI

Your pipeline needs the same two credentials to restore, and the licence key if it runs the application
or its integration tests. Store them as secrets.

In **GitHub Actions**, a workflow's own `GITHUB_TOKEN` cannot read packages from another organisation's
private feed, so pass the token as a secret:

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    env:
      TENANTRY_GITHUB_USERNAME: ${{ vars.TENANTRY_GITHUB_USERNAME }}
      TENANTRY_GITHUB_PAT: ${{ secrets.TENANTRY_GITHUB_PAT }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
        env:
          Tenantry__License: ${{ secrets.TENANTRY_LICENSE }}
```

Other CI systems work the same way: set `TENANTRY_GITHUB_USERNAME` and `TENANTRY_GITHUB_PAT` for the
restore, and `Tenantry__License` for anything that starts the application.

In a **Docker build**, pass the token as a build secret rather than a build argument, so it is not kept in
an image layer:

```dockerfile
RUN --mount=type=secret,id=tenantry_pat,env=TENANTRY_GITHUB_PAT \
    TENANTRY_GITHUB_USERNAME=your-github-username dotnet restore
```

```bash
docker build --secret id=tenantry_pat,env=TENANTRY_GITHUB_PAT .
```

## Rotating credentials

| Situation | What to do |
|-----------|------------|
| The token is about to expire | Create a new classic token with `read:packages` from the same account, then update the environment variable and the CI secret. Nothing else changes. |
| The token may have leaked | [Revoke it on GitHub](https://github.com/settings/tokens) straight away, then create a new one. A token only reads packages, but anyone holding it can download Pro. |
| You want a different GitHub account to hold access | Sign in to the new account on github.com, choose **Use another GitHub account** on your Pro access page, and accept the new account's invitation. The previous account keeps access until the new one is connected, then loses it, so create the token from the new account and update `TENANTRY_GITHUB_USERNAME`. |
| The licence key | It does not expire and stays the same through renewals, so it needs no rotation. If your subscription ends and you subscribe again, the Pro access page shows a new key. |

Access to the feed follows **one GitHub account per subscription**: the one connected on the Pro access
page. For CI, use a token from that account (or connect a dedicated machine account and use its token).

## When your subscription ends

The connected account is removed from `tenantry-org`, so restores from the GitHub feed fail. The versions you
already have keep working with your key. Keep copies of the packages you use, for example in your own internal feed
or a committed local package folder. Until the Tenantry feed replaces it, email support@tenantry.dev for the latest
patch of a minor version released while you subscribed.

## Troubleshooting

| Symptom | Likely cause |
|---------|--------------|
| `NU1101: Unable to find package Tenantry.Pro. No packages exist with this id in source(s): tenantry-pro` | The feed did not let this token see the package. GitHub reports a bad token this way rather than with a 401, so check in order: the environment variables are set in this shell or CI step; the token is a classic token with `read:packages` and has not expired or been revoked; it belongs to the account connected on the Pro access page; that account has accepted the `tenantry-org` invitation (the Pro access page shows its state); the subscription is active. |
| `401 Unauthorized` or `403 Forbidden` from `nuget.pkg.github.com` | The same checks as above. |
| `NU1101: Unable to find package Tenantry.Pro` naming only nuget.org | The `tenantry-pro` source or its `packageSourceMapping` patterns are missing, or another `nuget.config` with `<clear />` is taking precedence. `dotnet nuget list source` shows the sources in effect. |
| `LicenseRequiredException` at startup | No licence key reached the `Tenantry:License` setting, or it was mistyped. See [Licensing](licensing.md). |

## See also

- [Getting started](getting-started.md) — which packages to install, and building a first app.
- [Licensing](licensing.md) — how the key is validated, and what happens when a subscription ends.
