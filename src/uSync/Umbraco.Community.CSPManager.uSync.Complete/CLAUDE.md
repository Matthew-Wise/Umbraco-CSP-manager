# CSP Manager - uSync Complete (Publisher Support)

Extends the base uSync integration with Push/Pull capabilities for cross-environment sync.

## Components

- **Client/** - TypeScript/Lit backoffice UI for push/pull actions
  - `npm run build` / `npm run watch` for client build
  - Output: `wwwroot/App_Plugins/uSyncCspComplete/`
  - Uses `@jumoo/usync-publisher-assets` for UI components
- **Constants.cs** - API name: `usynccspmanagercomplete`

## Entity Actions

- Push: `usync.cspmanager.push.action` (kind: `publisher-push`)
- Pull: `usync.cspmanager.pull.action` (kind: `publisher-pull`)
- Targets entity types: `csp-policy` and `csp-manager-root`

## Conditional References

- Default: `ProjectReference` to CSPManager.uSync (dev)
- NuGet pack: `dotnet pack -p:UseProjectReferences=false` switches to `PackageReference`
- Version override: `-p:USyncMinVersion=X.Y.Z` (default: 3.0.0)

## Test Sites

- uSync.TestSite: port 44381 - primary test environment
- uSync.TestSiteB: port 44382 - second environment for push/pull testing
