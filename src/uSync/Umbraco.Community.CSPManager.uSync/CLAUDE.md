# CSP Manager - uSync Integration (Base)

Provides uSync serialization and sync support for CSP Manager configurations.

## Components

Two handlers, so global policies and node policies import in different uSync groups:

| | Global policies (Frontend, Backoffice) | Node policies |
|---|---|---|
| Handler | `CspDefinitionHandler` (Settings, 3000) | `CspNodePolicyHandler` (Content, 3010) |
| Serializer / XML element | `CspDefinitionSerializer` / `csp-policy` | `CspNodePolicySerializer` / `csp-node-policy` |
| Entity type / folder | `csp-policy` / `CspDefinitions` | `csp-node-policy` / `CspNodePolicies` |
| uSync key | fixed definition id | `CspItemKeys.NodePolicy(contentKey, culture)` (name-based v5 GUID; the all-cultures policy keeps the content-key-only name) |
| Alias | `front-end`, `backoffice` | `node-{contentKey}`, or `node-{contentKey}-{culture}` for one culture |

- **Handlers/CspHandlerBase.cs** - shared export-on-save / delete marker logic; picks its own serializer
  (uSync otherwise takes the first `ISyncSerializer<CspDefinition>`) and sets `TypeName`, which routes
  files to a handler by XML element during paged imports
- **Serializers/CspSerializerBase.cs** - shared XML format; sources have no `definitionId` (random per environment)
- **Sync/CspDependencyChecker.cs** - node policies depend on their content node
- **Sync/CspSyncItemManager.cs** - Publisher: maps tree items (definition ids) to handler UDIs
- **Trackers/CspDefinitionTracker.cs** - Tracks changes to CSP properties for diff detection
- **Composer.cs** - Registers notification handlers and both UDI types

Why not one handler: uSync finds a handler by entity type (`FirstOrDefault`), so a split needs two
entity types. Publisher only writes orphan-delete markers for the entity type the push started from
(`csp-policy` for the tree root), so a root push can't remove node policies missing on the source.

## Conditional References

- Default: `ProjectReference` to CSPManager (instant change flow during development)
- NuGet pack: `dotnet pack -p:UseProjectReferences=false` switches to `PackageReference`
- Version range: `$(CspManagerDependencyRange)`, defined in `src/Directory.Build.props`
  (default `[18.2.0, 19.0.0)`, see "Dependency range" in the root CLAUDE.md). The release workflow overrides it
  for prerelease builds; bump `CspManagerDependencyFloor`, not this csproj, on a breaking change.

## Test Sites

- `src/uSync/uSync.TestSite` (port 44381) and `uSync.TestSiteB` (port 44382), see the
  uSync.Complete CLAUDE.md. Run with the same command as the main test site, changing the project.

## Key Behaviors

- Serializes: Enabled, ReportOnly, ReportUri, ReportingDirective, UpgradeInsecureRequests, Sources, and for node policies `ContentKey`, plus `Culture`, `Inheritance` and `AppliesTo` when they aren't the default (so a complete all-cultures policy's file is unchanged from before they existed)
- Node policies: matched on import by content key and culture; created (with a local id) if the content node (and, for a culture, the language) exists on the target, otherwise the item fails with a reason
- Validation: `DeserializeCoreAsync` runs `CspDefinitionValidator` and fails items containing invalid tokens or directives
- Deletions: global policies cannot be deleted; node policy delete markers delete policies on import
