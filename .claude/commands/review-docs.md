# Review documentation pages

You are auditing CSP Manager documentation pages for quality, consistency, and accuracy.

## Scope

The user may specify:
- A single page: e.g., `docs/features/nonce-tag-helper.md`
- A section: e.g., `docs/guide/`
- `all` or no scope: review every page under `docs/` (excluding `_site/`, `assets/`, `_sass/`, `_includes/`)

## Before reviewing

1. Read `docs/CLAUDE.md` for the full style guide
2. Read `docs/_config.yml` for site structure and `exclude` list

## Checklist per page

### Front matter
- [ ] Has `title` and `nav_order`
- [ ] Child pages have `parent:` matching the parent page's `title` exactly
- [ ] Section index pages have `has_children: true`

### Content quality
- [ ] Active voice, second person ("you"), present tense
- [ ] Headings use sentence case (not title case)
- [ ] Short paragraphs (2-3 sentences max)
- [ ] Code examples present for all features and configuration options
- [ ] Code blocks have language tags (`csharp`, `json`, `html`, `bash`, etc.)
- [ ] Fenced code blocks used (not indented)
- [ ] Tables used for structured comparisons (not long bullet lists)

### Links
- [ ] Internal links use relative paths (not absolute docs site URLs)
- [ ] Internal link targets exist (check the file at the linked path)
- [ ] No broken anchor references

### Images
- [ ] Images in docs site pages use relative paths to `docs/assets/images/`
- [ ] Referenced image files exist at the stated path
- [ ] Images have descriptive alt text (not empty or generic)

### Callouts
- [ ] Only uses `{: .note }`, `{: .warning }`, `{: .highlight }` callout classes
- [ ] Callouts used for gotchas and caveats, not for general content

### Accuracy
- [ ] Configuration options match what exists in code
- [ ] Code examples use current API patterns (e.g., `builder.Services` not standalone `services`)
- [ ] Feature descriptions match current behaviour (check recent git commits for relevant changes)

## Cross-page checks

- Orphaned pages (no parent with `has_children: true`)
- Duplicate content between pages that should be consolidated
- Missing cross-references to related pages
- nav_order gaps or duplicates within a section

## Output format

For each page reviewed:

### `path/to/page.md`
**Status**: Pass / Needs attention / Needs rewrite

**Issues:**
1. [high/medium/low] Description — suggested fix

**Missing content:**
- What should be added

---

End with a summary:
- Pages reviewed / passing / needing attention
- Top 3 highest-priority fixes across the whole site
