# Umbraco Community CSP Manager

CSP management package for Umbraco CMS. Monorepo with 3 independently releasable NuGet packages.

## Project Structure

```
src/
├── Umbraco.Community.CSPManager/          # Main package (NuGet)
│   └── Client/                            # TypeScript/Lit frontend
├── Umbraco.Community.CSPManager.TestSite/ # Dev/testing site (port 44370)
├── Umbraco.Community.CSPManager.Tests/    # Unit/integration tests
├── Umbraco.Community.CSPManager.Benchmarks/
├── Directory.Build.props                  # Shared NuGet metadata
├── Directory.Packages.props               # Central package versioning
└── uSync/                                 # uSync integration packages
    ├── Umbraco.Community.CSPManager.uSync/           # Base uSync serialization
    ├── Umbraco.Community.CSPManager.uSync.Complete/  # uSync Publisher push/pull
    ├── uSync.TestSite/                    # Test site A (port 44381)
    └── uSync.TestSiteB/                   # Test site B (port 44382)
```

## Tech Stack

- Backend: .NET 10, Umbraco 17+, NPoco ORM
- Frontend: Lit 3.x, Vite 7.x, Node 22+
- Testing: NUnit (backend), Playwright (frontend)
- API: OpenAPI code generation via @hey-api/openapi-ts

## CI Release Tags

All packages use major version aligned to Umbraco (e.g. Umbraco 17 → `17.x.x`).

- CSP Manager: GitHub Release tag `17.0.0` (triggers `csp-manager.yml`)
- uSync: git tag `usync-17.0.0` (triggers `usync.yml` `release-usync` job)
- uSync Complete: git tag `usync-complete-17.0.0` (triggers `usync.yml` `release-usync-complete` job)

Each package releases independently. Dependencies use a version range `[17.0.0-0, 18.0.0)` — accepts any 17.x including pre-releases. Only update the lower bound in the `.csproj` when a dependency has a breaking change that requires a newer minimum.

## Development Principles

- Security by Design: all code should enhance security, never compromise it
- Content-Like Management: CSP policies as easy to manage as Umbraco content
- Performance: minimal impact on request processing and page load times
- Extensibility: support custom integrations via notification system
