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

- Backend: .NET 10, Umbraco 18+, NPoco ORM
- Frontend: Lit 3.x, Vite 7.x, Node 22+
- Testing: NUnit (backend), Playwright (frontend)
- API: OpenAPI code generation via @hey-api/openapi-ts

## Releasing

All packages use major version aligned to Umbraco (e.g. Umbraco 18 → `18.x.x`).

Single entry point: **Actions → Release → Run workflow** (`release.yml`). Pick the
`package` (`all` / `csp-manager` / `usync` / `usync-complete`) and a `version`
(e.g. `18.0.0` or `18.0.0-beta-1`). Each package is packed, pushed to NuGet, and
gets its own tag + titled GitHub Release:

- CSP Manager → tag `csp-manager-<version>`, release "CSP Manager <version>"
- uSync → tag `usync-<version>`, release "uSync <version>"
- uSync Complete → tag `usync-complete-<version>`, release "uSync Complete <version>"

`all` packs in dependency order (main → uSync → uSync.Complete) against a local
NuGet feed, so a brand-new version resolves without waiting for nuget.org indexing.
Each package still releases independently — patch one without re-releasing the others
by selecting just that package.

**Versions are immutable — never re-release one.** NuGet rejects a duplicate version
and GitHub releases are immutable, so a published version can't be replaced; to ship a
change, bump the version (e.g. `18.0.0-beta-1` → `18.0.0-beta-2`). The publish job is
re-run-safe (NuGet push uses `--skip-duplicate`; the release step skips if the tag
already exists), so re-running after a *partial* failure completes the missing steps
without erroring — but it won't overwrite anything already published.

**Deployment environments:** the `workflow_dispatch` runs from `main`, so each
`nuget-*` environment's protection rules must allow the `main` branch (Settings →
Environments). The old tag-triggered flow only allowed `usync-*` tags, so this needs
adding once per environment.

`csp-manager.yml` / `usync.yml` now only run build + test on push/PR. They pack a
unique prerelease version `0.0.0-ci.<run_number>` and upload the `.nupkg`s as
artifacts (`nuget-packages` / `uSync Build Output`) so a build can be tested before
merge — the unique version stops NuGet serving a stale cached `0.0.0`:

```
gh run download <run-id> -n nuget-packages
dotnet nuget add source ./<downloaded-folder> -n pr-test
dotnet add package Umbraco.Community.CSPManager -v 0.0.0-ci.<run_number> --prerelease
```

**Dependency range:** the supporting packages declare `[18.0.0, 19.0.0)` for their
internal CSP Manager dependencies, assembled in `src/Directory.Build.props` from
`CspManagerDependencyFloor` (`18.0.0`) and `CspManagerDependencyCeiling` (`19.0.0`).
Bump the floor only on a breaking change that requires a newer minimum. The release
workflow overrides the floor to the exact version for prerelease builds (e.g.
`[18.0.0-beta-1, 19.0.0)`) — no manual csproj edits for betas. Floor/ceiling are
separate (no commas) because .NET's `-p:` parser splits property values on commas.

**NuGet trusted publishing:** each package's policy must reference workflow
`release.yml` (environments `nuget-csp` / `nuget-usync` / `nuget-usync-complete`
are unchanged).

## Development Principles

- Security by Design: all code should enhance security, never compromise it
- Content-Like Management: CSP policies as easy to manage as Umbraco content
- Performance: minimal impact on request processing and page load times
- Extensibility: support custom integrations via notification system
