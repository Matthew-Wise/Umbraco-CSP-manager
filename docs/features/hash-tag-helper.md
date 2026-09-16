---
title: Hash Tag Helper
parent: Features
nav_order: 2
---

# Hash Tag Helper

CSP hashes allow you to whitelist specific **static** inline scripts and styles without using `'unsafe-inline'`. CSP Manager provides a tag helper that computes the SHA-256 hash of a tag's content at render time and includes it in the CSP header — no manual hashing, and no per-request value to keep in sync.

For background on hashes, see [Caching & Nonces](../advanced/caching#option-3-use-hashes-instead-of-nonces).

{: .warning }
Hashes only work when the inline content is **identical on every request**. Use this tag helper for static content only — never for content that includes per-request or per-user data (user names, timestamps, generated tokens). For dynamic inline content, use the [Nonce Tag Helper](nonce-tag-helper) instead.

## Setup

Add the tag helper namespace to your `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Umbraco.Community.CSPManager
```

## Adding a Hash

Add `csp-manager-add-hash="true"` to a `<script>` or `<style>` tag with static inline content:

```html
<script csp-manager-add-hash="true">
  console.log("hello");
</script>

<style csp-manager-add-hash="true">
  .alert { color: red; }
</style>
```

CSP Manager hashes the tag's exact content and includes it in the `script-src` or `style-src` directive of the CSP header:

```
Content-Security-Policy: script-src 'self' 'sha256-<base64-hash>'; style-src 'self' 'sha256-<base64-hash>'
```

The tag helper does not modify the rendered tag itself — unlike a nonce, a hash source does not need to appear anywhere in the HTML, so the tag is left as-is (aside from an optional [data attribute](#hash-as-a-data-attribute)).

{: .note }
This tag helper only applies to inline content. It has no effect on a `<script src="...">` tag — a `src` attribute means the browser needs a hash of the fetched resource, which this tag helper does not compute — and a warning is logged if you use it on one.

### Policies using `script-src-elem` or `style-src-elem`

Just like nonces, CSP Manager adds the hash to every configured directive in each `-src` / `-src-elem` pair, so hashed tags work in browsers that support the `-elem` variant and those that don't:

| Configured directives | Hash added to |
|---|---|
| `script-src` | `script-src` |
| `script-src-elem` | `script-src-elem` |
| `script-src` and `script-src-elem` | both |

The same applies to `style-src` and `style-src-elem`. If neither directive in a pair is configured, the hash is not added and a warning is logged, because creating the directive from scratch would block every other source.

## Hash as a Data Attribute

If you need to read the computed hash value (e.g. for diagnostics), add `csp-manager-add-hash-data-attribute="true"` alongside `csp-manager-add-hash="true"`:

```html
<script csp-manager-add-hash="true" csp-manager-add-hash-data-attribute="true">
  console.log("hello");
</script>
```

{: .note }
The tag helper only runs on tags that carry `csp-manager-add-hash`. On its own, `csp-manager-add-hash-data-attribute` does nothing.

This adds a `data-csp-hash` attribute containing the hash value without the surrounding quotes:

```html
<script data-csp-hash="sha256-...">
  console.log("hello");
</script>
```

## How Hashes Work

- The tag helper reads the tag's exact rendered content and computes its SHA-256 hash
- Multiple hashed `<script>`/`<style>` blocks on the same page each contribute their own hash to the header
- Computed hashes are cached for the lifetime of the process, keyed by content — the first request to render a given block pays the hashing cost, every later request (and every other template rendering the same static content) reuses the cached value
- Because the hash is a function of the content itself, it is stable across requests, output caching, and CDN edges — unlike a nonce, there is nothing that needs to match a per-request value
- Hashes are added regardless of whether the policy is in enforcing or report-only mode
