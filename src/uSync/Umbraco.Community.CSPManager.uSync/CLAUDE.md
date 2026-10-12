# CSP Manager - uSync Integration (Base)

Provides uSync serialization and sync support for CSP Manager configurations.

## Components

- **Serializers/CspDefinitionSerializer.cs** - CSP definition to/from XML serialization
- **Handlers/CspDefinitionHandler.cs** - `SyncHandlerRoot<CspDefinition>`, exports on `CspSavedNotification`,
  writes a delete marker on `CspDeletedNotification`
- **Trackers/CspDefinitionTracker.cs** - Tracks changes to CSP properties for diff detection
- **CspItemNames.cs** - aliases/names: `backoffice`, `front-end`, `node-{contentKey}`
- **Composer.cs** - Registers notification handlers

## Conditional References

- Default: `ProjectReference` to CSPManager (instant change flow during development)
- NuGet pack: `dotnet pack -p:UseProjectReferences=false` switches to `PackageReference`
- Version range: `$(CspManagerDependencyRange)`, defined in `src/Directory.Build.props`
  (default `[18.0.0, 19.0.0)` — accepts any 18.x). The release workflow overrides it
  for prerelease builds; bump `CspManagerDependencyFloor`, not this csproj, on a breaking change.

## Key Behaviors

- Serializes: Enabled, ReportOnly, ReportUri, ReportingDirective, UpgradeInsecureRequests, Sources, and `ContentKey` (domain policies only; the content node's key, stable across environments)
- Items: backoffice, frontend, and all domain policies under uSync group "Settings"
- Domain policies: created on import if the content node exists on target; updates existing policy if present; fails if reassigning to another content node
- Validation: `DeserializeCoreAsync` runs `CspDefinitionValidator` and fails items containing invalid tokens or directives
- Deletions: global policies cannot be deleted; domain policy delete markers delete policies on import
