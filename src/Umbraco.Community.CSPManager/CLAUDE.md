# CSP Manager - Main Package

## Architecture

- **Controllers/**: API endpoints at `/csp/api/v1.0` with custom authorization
- **Services/**: `ICspService` - core business logic, nonce generation, caching
- **Middleware/**: `CspMiddleware` injects CSP headers via `Response.OnStarting()` callback
- **Models/**: `CspDefinition` (NPoco entity), `CspDefinitionSource`, API DTOs
- **Notifications/**: `CspSavedNotification`, `CspWritingNotification` for extensibility
- **TagHelpers/**: `CspNonceTagHelper` for `<script csp-manager-add-nonce>` and `<style>` tags;
  `CspHashTagHelper` for `<script csp-manager-add-hash>` and `<style>` tags with static content

## Key Patterns

- Dual context: separate policies for frontend (`fac780be-...`) and backoffice (`9cbfa28c-...`)
- Cache-first retrieval with distributed cache invalidation on save
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
- Hash-per-content, not per-request: `CspHashTagHelper` hashes a tag's exact static content via
  `ICspService.AddCspHash`, which caches the `'sha256-...'` source in a process-wide
  `ConcurrentDictionary` keyed by content, so identical content is hashed once regardless of which
  template renders it or how many requests follow. Computed hashes accumulate per-request in
  `CspManagerContext.ScriptHashes`/`StyleHashes` (read via the non-creating
  `HttpContext.GetCspManagerContext()`, unlike the nonce's boolean marker items) and follow the
  same directive-pairing and never-create-from-scratch rules as nonces.
- Middleware never breaks requests on failure
- Composer pattern: `CspManagerComposer` auto-registers via `IComposer`

## API Endpoints

- `GET /csp/api/v1.0/Definitions?isBackOffice=false` - retrieve CSP definition
- `POST /csp/api/v1.0/Definitions/save` - save CSP definition

## Configuration

- `CspManagerOptions.DisableBackOfficeHeader` - disable CSP on backoffice
- All CSP directives defined in `Constants.cs`
