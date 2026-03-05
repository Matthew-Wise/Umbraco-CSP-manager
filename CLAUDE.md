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

- CSP Manager: GitHub Release tag format `3.1.0`
- uSync: tag `usync-3.0.2`
- uSync Complete: tag `usync-complete-3.0.2`

## Development Principles

- Security by Design: all code should enhance security, never compromise it
- Content-Like Management: CSP policies as easy to manage as Umbraco content
- Performance: minimal impact on request processing and page load times
- Extensibility: support custom integrations via notification system
