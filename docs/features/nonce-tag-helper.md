---
title: Nonce Tag Helper
parent: Features
nav_order: 1
---

# Nonce Tag Helper

CSP nonces allow you to whitelist specific inline scripts and styles without using `'unsafe-inline'`. CSP Manager provides a tag helper that automatically injects a cryptographically secure per-request nonce into your tags and includes it in the CSP header.

For background on nonces, see the [nonce guide on content-security-policy.com](https://content-security-policy.com/nonce/).

{: .warning }
Nonces require a unique value per request. If your pages are served through a CDN or reverse proxy that caches HTML, cached nonces will not match the CSP header and inline scripts/styles will be blocked. See [Caching & Nonces](../advanced/caching) before enabling nonces in a cached environment.

## Setup

Add the tag helper namespace to your `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Umbraco.Community.CSPManager
```

## Adding a Nonce

Add `csp-manager-add-nonce="true"` to any `<script>`, `<style>`, or `<link>` tag:

```html
<script csp-manager-add-nonce="true">
  doWhatever();
</script>

<style csp-manager-add-nonce="true">
  .alert { color: red; }
</style>

<link csp-manager-add-nonce="true" rel="stylesheet" href="/styles.css">
```

The tag helper injects a `nonce` attribute with a unique value, and CSP Manager includes the matching nonce in the `script-src` or `style-src` directive of the CSP header:

```html
<!-- Rendered output (nonce values are auto-generated per request): -->
<script nonce="abc123xyz">
  doWhatever();
</script>

<style nonce="abc123xyz">
  .alert { color: red; }
</style>

<link nonce="abc123xyz" rel="stylesheet" href="/styles.css">
```

{: .note }
A single nonce is shared across all `<script>`, `<style>`, and `<link>` tags in the same request. CSP Manager adds it to both `script-src` and `style-src` in the response header.

### Policies using `script-src-elem` or `style-src-elem`

`script-src-elem` and `style-src-elem` override `script-src` and `style-src` for element-level scripts and styles, so they are the only directives a browser consults for a `<script>`, `<style>`, or `<link>` tag. When your policy defines one of them, CSP Manager adds the nonce there instead of to the broader directive:

| Configured directives | Nonce added to |
|---|---|
| `script-src` | `script-src` |
| `script-src-elem` | `script-src-elem` |
| `script-src` and `script-src-elem` | `script-src-elem` |

The same applies to `style-src` and `style-src-elem`. The nonce is deliberately not added to both — a nonce in `script-src` makes the browser ignore `'unsafe-inline'` there, which would break the inline event handlers that `script-src-attr` falls back to.

## Nonce as a Data Attribute

If you need to read the nonce value in JavaScript (e.g., to dynamically create elements), use `csp-manager-add-nonce-data-attribute="true"`:

```html
<script csp-manager-add-nonce-data-attribute="true"></script>
<style csp-manager-add-nonce-data-attribute="true"></style>
```

This adds a `data-nonce` attribute alongside the `nonce` attribute:

```html
<script nonce="abc123xyz" data-nonce="abc123xyz"></script>
<style nonce="abc123xyz" data-nonce="abc123xyz"></style>
```

## How Nonces Work

- A single nonce is generated per HTTP request using a cryptographically secure random number generator
- The same nonce value is used for all `<script>`, `<style>`, and `<link>` tags on the page
- The nonce is automatically included in both `script-src` and `style-src` in the outgoing `Content-Security-Policy` header — or in `script-src-elem` / `style-src-elem` when those are configured
- Nonces are generated regardless of whether the policy is in enforcing or report-only mode
