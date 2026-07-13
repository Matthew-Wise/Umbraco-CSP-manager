# CSP Manager - uSync Integration (Base)

Provides uSync serialization and sync support for CSP Manager configurations.

## Components

- **Serializers/CspDefinitionSerializer.cs** - CSP definition to/from XML serialization
- **Handlers/CspDefinitionHandler.cs** - `SyncHandlerRoot<CspDefinition>`, responds to `CspSavedNotification`
- **Trackers/CspDefinitionTracker.cs** - Tracks changes to CSP properties for diff detection
- **Composer.cs** - Registers notification handler

## Conditional References

- Default: `ProjectReference` to CSPManager (instant change flow during development)
- NuGet pack: `dotnet pack -p:UseProjectReferences=false` switches to `PackageReference`
- Version range: `[18.0.0, 19.0.0)` — accepts any 18.x

## Key Behaviors

- Serializes: Enabled, ReportOnly, ReportUri, ReportingDirective, UpgradeInsecureRequests, Sources
- Returns two child items: backoffice and frontend CSP definitions
- uSync group: "Settings"
- Prevents deletion of CSP definitions (returns empty task)
