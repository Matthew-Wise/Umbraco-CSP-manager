# CSP Manager - Main Package

## Architecture

- **Controllers/**: API endpoints at `/umbraco/csp/api/v1` with custom authorization
- **Services/**: `ICspService` - core business logic, nonce generation, caching
- **Middleware/**: `CspMiddleware` injects CSP headers via `Response.OnStarting()` callback
- **Models/**: `CspDefinition` (NPoco entity), `CspDefinitionSource`, API DTOs
- **Notifications/**: `CspSavedNotification`, `CspDeletedNotification`, `CspWritingNotification`, `CspHeaderConstructionFailedNotification` for extensibility
- **TagHelpers/**: `CspNonceTagHelper` for `<script csp-manager-add-nonce>` and `<style>` tags

## Key Patterns

- Dual context: separate policies for frontend (`fac780be-...`) and backoffice (`9cbfa28c-...`)
- Node policies: rows with `ContentKey`. Any content node can have one, per culture: the scope of a
  policy is (`ContentKey`, `Culture`), `Culture` null meaning all cultures. The public API says "node"
  (`GetCspDefinitionForNodeAsync`, `DisabledNodePolicyBehavior`, `node-policies`) and so does the UI
  ("node policy"). Backoffice always uses the backoffice policy.
- Inheritance: `Inheritance` is `Replace` (a complete policy, the default and what every policy was
  before this existed) or `Extend` ("inherit and add": only extra sources, merged onto the policy it
  inherits). `AppliesTo` is `Descendants` (the node and the pages below it, the default) or `NodeOnly`.
- Resolution (`CspNodePolicyResolver`, pure logic, called by `ICspService.ResolveCspDefinitionAsync`):
  candidates from most to least specific are the requested page, then each ancestor nearest first
  (`IDocumentNavigationQueryService`, in memory). When the node of the matched hostname isn't among
  them (content served from outside the site, e.g. a shared 404 page stored under another site),
  the hostname's node and its ancestors take the place of the page's ancestors: the visitor is on
  that site, so its policies apply. On each node: the request
  culture (`PublishedRequest.Culture`), then its language fallback chain (`ILanguage.FallbackIsoCode`),
  then the all-cultures policy. Ancestor policies only count with `AppliesTo == Descendants`. Enabled
  `Extend` policies are collected; the walk ends at the first enabled `Replace` policy (the base), or
  at the global Frontend policy. A disabled policy is skipped, except that a disabled `Replace` policy
  under `DisabledNodePolicyBehavior.NoHeader` ends the walk with no header. The merged result has the
  id, node and culture of the most specific policy, the settings (enabled, report-only, reporting,
  upgrade-insecure-requests) of the base and the sources of all of them; `'none'` is dropped from a
  directive that gained another source. An `Extend` policy only ever allows more: a fetch directive
  it is the first to use (`script-src` on a base with only `default-src`) starts from the sources of
  the directive it fell back to, and is left out when it fell back to nothing (the base doesn't
  restrict it). Directives without a fallback (`frame-ancestors`, `form-action`) are added as written.
- Keyed on the node, not the hostname: Umbraco doesn't persist `IDomain.Key`, hostnames differ per
  environment, and a multilingual site has a hostname per culture on one node. A policy is orphaned
  (kept, no effect, deletable) when its node is deleted or trashed, or its culture is no longer a
  language. `CspDomainNodeLookup` reads node names/hostnames for the API.
- Identity & locking: `EnsureValidIdentityShape` and `EnsureValidNodePolicyAsync` enforce one policy per
  node and culture (also two filtered unique indexes, one for the all-cultures policy and one for
  culture policies, since SQLite and SQL Server disagree on NULLs in a unique index), assign GUIDs if
  empty, keep `ContentKey` and `Culture` immutable, store a new policy's culture in the language's own
  spelling, and reject content keys, cultures and `Extend` on the global ids. Writes take
  `scope.EagerWriteLock(Constants.Locks.Definitions)` (`-2776`) to prevent SQLite deadlocks.
- Backward compatibility: 18.1.0 (the last release without node policies) ends its migration plan at
  `UpgradeInsecureRequestsMigration`. Everything after is additive (`DomainPolicyMigration`,
  `DefinitionsLockMigration`, `NodePolicyScopeMigration`), so existing rows stay valid with the
  defaults (null culture, `Replace`, `Descendants`). Never change a released migration; add a new one.
- Header validation: `CspDefinitionValidator` validates sources, directives, and reporting URIs across API, service, and uSync. Whitespace sources are pruned; invalid stored tokens are skipped during response generation.
- Caching: all node policies are cached as one snapshot (`csp-node-policies`; empty on most sites, so
  a request then costs one cache lookup), the language fallback chains under
  `csp-node-language-fallbacks` (cleared by `CspLanguageCacheHandler` on Umbraco's
  `LanguageCacheRefresherNotification`). Both share the `csp-node-` prefix that `RefreshAll` clears.
  Ancestors are never cached: they come from Umbraco's live navigation structure. Saves and deletions
  publish `CspSavedNotification`/`CspDeletedNotification` post-commit and trigger a distributed refresh.
- Cache invalidation ordering is easy to regress: `GetCachedCspDefinitionAsync` caches the
  in-flight `Task` (not the awaited result) so a concurrent save can't overwrite an invalidation
  with a stale result; `SaveCspDefinitionAsync` publishes `CspSavedNotification` only after the
  scope disposes (post-commit); invalidation broadcasts to every server, not just the scheduling
  publisher, since a save can land on any of them.
- `GetCachedCspDefinitionAsync` returns a defensive copy (`CloneDefinition`) of the cached
  instance, never the cached reference itself - `CspWritingNotification` hands the result to
  consumer code, and the documented handler pattern mutates `CspDefinition.Sources` directly, so
  returning the shared reference would let one handler's mutation corrupt what every other
  request sharing the cache sees.
- Nonce-per-request: cryptographically secure, reused within HTTP context
- Nonce directive targeting: the nonce goes on every configured directive in the
  `script-src`/`script-src-elem` and `style-src`/`style-src-elem` pairs. Supporting browsers consult
  the `-elem` variant for `<script>`/`<style>`/`<link>` and ignore the broader one; older browsers only
  know the broader one, so both need it. A directive is never created just to hold a nonce (that would
  block every other source); if neither in a pair exists, `CspNonceDirectiveMissing` is logged.
  Inline event handlers/style attributes with `'unsafe-inline'` belong in `script-src-attr`/`style-src-attr`.
- Middleware never breaks requests on failure
- Composer pattern: `Composer` (`Composers/CspManagerComposer.cs`) auto-registers via `IComposer`

## API

Routes live in `Controllers/` under `/umbraco/csp/api/v1` and are described by the OpenAPI document
(`/umbraco/openapi/csp.json`, the source of the generated client). What the routes don't show:

- Stored versus effective: `GET Definitions?contentKey=&culture=` returns the policy stored for exactly
  that node and culture (no fallback, 404 if none); `GET Definitions/effective` returns what a request
  for that page and culture gets today (resolved and merged, or the Frontend policy). No culture
  means the default language, as for a routed request. A merged result is not a stored policy: it
  has an empty id, so it can't be saved over the `Extend` policy it is named after.
- `isOrphaned` on a definition and in the `node-policies` list is display only (node deleted or
  trashed, or culture no longer a language). `Nodes` lists the nodes with a hostname (the sites);
  `Nodes/{contentKey}` returns any content node, 404 when it is missing or trashed.
- Creating: a node policy posted to `Definitions/save` with `Guid.Empty` is created with a
  server-assigned id; any other unknown node-policy id is 404. `create-from-frontend` creates a
  `Replace` policy as a copy of what the node gets today, an `Extend` policy without sources.
- The two global policies can't be deleted (400), and can't have a content key, a culture or `Extend`.

## Configuration

- `CspManagerOptions.DisableBackOfficeHeader` - disable CSP on backoffice
- `CspManagerOptions.FailureBehavior` - `FailOpen` (default, no header when construction throws) or
  `FailClosed` (minimal `default-src 'self'` fallback, frontend only; keeps the definition's report-only mode). Either way the request completes, and
  `CspHeaderConstructionFailedNotification` handlers can override the fallback per request.
- `CspManagerOptions.DisabledNodePolicyBehavior` - what a disabled node policy does: `FallbackToGlobal`
  (default: skipped, so the next policy up the tree or the Frontend policy applies) or `NoHeader`
  (a disabled complete policy sends no header)
- All CSP directives defined in `Constants.cs`
