# CSP Manager - Main Package

## Architecture

- **Controllers/**: API endpoints at `/csp/api/v1.0` with custom authorization
- **Services/**: `ICspService` - core business logic, nonce generation, caching
- **Middleware/**: `CspMiddleware` injects CSP headers via `Response.OnStarting()` callback
- **Models/**: `CspDefinition` (NPoco entity), `CspDefinitionSource`, API DTOs
- **Notifications/**: `CspSavedNotification`, `CspWritingNotification` for extensibility
- **TagHelpers/**: `CspNonceTagHelper` for `<script csp-manager-add-nonce>` and `<style>` tags

## Key Patterns

- Dual context: separate policies for frontend (`fac780be-...`) and backoffice (`9cbfa28c-...`)
- Cache-first retrieval with distributed cache invalidation on save
- Cache invalidation ordering matters and is easy to regress:
  - `GetCachedCspDefinitionAsync` caches the in-flight `Task`, not the awaited result, so the entry
    exists before the database load runs. Awaiting first and inserting afterwards lets a load that
    started before a save write its stale result back over the invalidation — and these entries have
    no expiry, so the old policy is then served until the site recycles.
  - `SaveCspDefinitionAsync` publishes `CspSavedNotification` only after the scope is disposed, i.e.
    after the transaction commits, so nothing can reload pre-save rows into the cache.
  - Invalidation is broadcast for every server role: a save is handled by whichever server the
    editor is on, not necessarily the scheduling publisher.
- Nonce-per-request: cryptographically secure, reused within HTTP context
- Middleware never breaks requests on failure
- Composer pattern: `CspManagerComposer` auto-registers via `IComposer`

## API Endpoints

- `GET /csp/api/v1.0/Definitions?isBackOffice=false` - retrieve CSP definition
- `POST /csp/api/v1.0/Definitions/save` - save CSP definition

## Configuration

- `CspManagerOptions.DisableBackOfficeHeader` - disable CSP on backoffice
- All CSP directives defined in `Constants.cs`
