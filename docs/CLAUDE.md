# Docs Site — Jekyll / Just the Docs

This directory is a [Just the Docs](https://just-the-docs.com/) Jekyll site deployed to GitHub Pages at `https://matthew-wise.github.io/Umbraco-CSP-manager/`.

## Critical: Jekyll renders HTML as a single line

Jekyll collapses the entire rendered page into a single line of HTML. This has one important consequence for any JavaScript written in `_includes` files:

**Never use `//` single-line comments in inline scripts.**

When the page is a single line, a `//` comment extends to the end of the page — commenting out all remaining script code, leaving unclosed braces, and causing `Unexpected end of input` parse errors. The script silently fails with no obvious cause.

```js
// BAD — kills everything after this point when rendered
jtd.setTheme(initialTheme);

/* OK — block comments are safe */
jtd.setTheme(initialTheme);
```

The same rule applies to any HTML `_includes` file that contains `<script>` blocks.

## _includes files are rendered twice

`_includes/nav_footer_custom.html` is included in two places by Just the Docs:

1. Desktop sidebar footer (`.d-md-block.d-none`)
2. Mobile page footer (`.d-md-none`)

This means any `<script>` block in that file runs twice. Handle this with:

- A `window.__guardFlag` check so one-time initialisation only runs once
- Event delegation on `document` rather than binding to individual elements by ID, to avoid double-binding

```js
(function () {
  if (window.__jtdThemeReady) { return; }
  window.__jtdThemeReady = true;
  /* ... one-time setup ... */
})();
```

## Theme switching

Just the Docs theme switching works by swapping the `<link rel="stylesheet">` href. `jtd.setTheme(name)` loads `/Umbraco-CSP-manager/assets/css/just-the-docs-{name}.css`.

The custom colour scheme CSS files map as follows:

| Toggle value | CSS file loaded              | SCSS source                          |
|--------------|------------------------------|--------------------------------------|
| `'light'`    | `just-the-docs-light.css`    | `assets/css/just-the-docs-light.scss` (uses `umbraco` scheme)      |
| `'dark'`     | `just-the-docs-dark.css`     | `assets/css/just-the-docs-dark.scss` (uses `umbraco-dark` scheme)  |

The colour schemes themselves are defined in `_sass/color_schemes/umbraco.scss` and `_sass/color_schemes/umbraco-dark.scss`.

## Style guide

### Content

- Write for Umbraco developers — assume .NET familiarity, not necessarily CSP expertise.
- Active voice, second person ("you"), present tense. Example: "Add the tag helper" not "The tag helper can be added".
- Lead with the practical outcome, not the mechanism.
- Keep pages focused: one topic per page.
- Short paragraphs (2-3 sentences max).

### Callouts

Just the Docs supports blockquote admonitions using Kramdown block attributes:

| Class | Use for |
|---|---|
| `{: .note }` | Gotchas and things to be aware of |
| `{: .warning }` | Important caveats that can cause problems |
| `{: .highlight }` | Tips and shortcuts |

Example:
```markdown
{: .warning }
Nonces break when pages are cached by a CDN.
```

### Markdown

- Headings: sentence case, not title case.
- Code blocks: always specify the language (`csharp`, `json`, `html`, `bash`, etc.).
- Use fenced code blocks (` ``` `), not indented blocks.
- Prefer tables over bullet lists for structured comparisons.

### Links

- Use relative links between pages in the docs site: `../advanced/caching`, `policy-settings`.
- Never use absolute docs site URLs (e.g. `https://matthew-wise.github.io/...`) for internal links.
- Use absolute URLs for external links (GitHub, NuGet, Umbraco docs, etc.).
- Use reference-style links at the bottom of the file when the same URL appears more than once.

### Images

- Store screenshots in `docs/assets/images/screenshots/` with kebab-case filenames.
- Reference screenshots with relative paths from the page: `../assets/images/screenshots/filename.png`.
- NuGet READMEs must use absolute `raw.githubusercontent.com` URLs — nuget.org does not resolve relative paths.
- Always include descriptive alt text.

### Front matter

Every page must have at minimum:

```yaml
---
title: Page Title
nav_order: N
parent: Parent Section   # omit for top-level pages
---
```

Section index pages must also include `has_children: true`. The `parent:` value must match the parent page's `title` exactly.

### File naming

- Lowercase, hyphen-separated: `nonce-tag-helper.md`, not `NonceTagHelper.md`.
- Place pages under the appropriate section folder (`guide/`, `features/`, `advanced/`, `integrations/`).
