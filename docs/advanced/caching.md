---
title: Caching & Nonces
parent: Advanced
nav_order: 2
---

# Caching & Nonces

{: .warning }
If your pages are cached by a CDN or reverse proxy, nonces will break. Read this page before enabling nonces in a production environment with caching.

## The Problem

CSP Manager generates a fresh cryptographically secure nonce on every request. The nonce is embedded in two places:

1. The `Content-Security-Policy` response header (e.g. `script-src 'nonce-abc123'`)
2. The `nonce="abc123"` attribute on each tagged HTML element

For a nonce to work, these two values **must match**. If a CDN serves a cached copy of the HTML, the nonce in the page is from the original request — but the CSP header is generated fresh by Umbraco for the current request. The values won't match and all inline scripts and styles will be blocked.

## Options

### Option 1: Disable caching for pages using nonces

The simplest fix is to tell CDNs and browsers not to cache responses that contain nonces:

```csharp
// In middleware or a filter:
context.Response.Headers["Cache-Control"] = "no-store";
```

Or configure your CDN to bypass caching for those routes. This is the safest approach but sacrifices caching performance.

### Option 2: Cloudflare Workers (nonce replacement at the edge)

A Cloudflare Worker can intercept responses and replace nonces consistently at the edge — rewriting both the HTML and the CSP header with a fresh per-request nonce. This lets you keep CDN caching for the rest of the response while ensuring nonces are unique.

The Worker needs to:
1. Generate a new random nonce for each request
2. Rewrite all `nonce="..."` attributes in the HTML body using Cloudflare's [`HTMLRewriter` API](https://developers.cloudflare.com/workers/runtime-apis/html-rewriter/)
3. Replace the nonce value in the `Content-Security-Policy` header to match

This keeps caching intact for the rest of the response while ensuring every visitor gets a unique nonce.

### Option 3: Use hashes instead of nonces

If your inline scripts or styles are static (i.e. their content does not change per request), you can use a CSP hash instead of a nonce. A hash is computed from the exact content of the script or style block and added to the CSP header — no per-request value needed, so caching is not a problem.

Example for an inline script:

```html
<script>doWhatever();</script>
```

Compute the SHA-256 hash of the script content (excluding the tags), then add it as a source:

```
Content-Security-Policy: script-src 'sha256-<base64-hash>'
```

Hashes can be added to a CSP policy in CSP Manager as a source value, e.g. `'sha256-abc123...'`.

{: .note }
Hashes only work when the inline content is **identical on every request**. Any dynamic content (user data, timestamps, generated values) means the hash won't match and the script will be blocked. Use nonces for dynamic inline content and hashes for static inline content.

### Option 4: Avoid inline scripts and styles

If caching is critical and edge rewriting is not practical, consider removing inline scripts and styles entirely in favour of external files. External files can be cached without affecting nonce validity, and do not require `'unsafe-inline'` or nonces.

## Recommendations

| Scenario | Recommendation |
|---|---|
| No CDN / origin-only | Nonces work as-is — no action needed |
| CDN with page caching, dynamic inline content | Use Cloudflare Workers nonce rewriting, or disable cache for HTML responses |
| CDN with page caching, static inline content | Use hashes — no per-request value, caching-safe |
| High-traffic site with full-page caching | Move to external scripts/styles — no nonces or hashes needed |

## Further Reading

- [Content Security Policy: Nonces](https://content-security-policy.com/nonce/)
- [HTMLRewriter — Cloudflare Workers docs](https://developers.cloudflare.com/workers/runtime-apis/html-rewriter/)
- [MDN: Content-Security-Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy)
