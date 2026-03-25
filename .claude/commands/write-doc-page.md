# Write a new documentation page

You are writing a documentation page for the CSP Manager for Umbraco docs site (Jekyll, Just the Docs theme).

## Input

The user will describe the topic for the new page. Ask clarifying questions if needed:
- Which section is unclear (`guide/`, `features/`, `advanced/`, `integrations/`, or top-level)
- What the `parent:` title should be (must match the parent page's `title` exactly)
- What `nav_order` to use (read existing sibling pages to find the next available number)

## Before writing

1. Read `docs/CLAUDE.md` for the full style guide
2. Read `docs/_config.yml` to understand site structure
3. Read all existing pages in the target section to determine the nav_order sequence and understand sibling content
4. Check the parent page has `has_children: true` in its front matter

## Front matter (required)

```yaml
---
title: Page Title
nav_order: N
parent: Parent Section Title
---
```

Omit `parent:` for top-level pages. For section index pages also include `has_children: true`.

## Content rules

- Write for Umbraco developers — assume .NET familiarity, not CSP expertise
- Active voice, second person ("you"), present tense
- Lead with the practical outcome, not the mechanism
- Short paragraphs (2-3 sentences max)
- One topic per page
- Include code examples for every feature, with fenced code blocks and language tags (`csharp`, `json`, `html`, `bash`)
- Use callouts for important information:
  - `{: .note }` — gotchas and things to be aware of
  - `{: .warning }` — important caveats that can cause problems
  - `{: .highlight }` — tips and shortcuts
- Headings: sentence case, not title case
- Prefer tables over bullet lists for structured comparisons
- Use relative links between docs pages (e.g., `../advanced/caching`)
- Use reference-style links at the bottom for URLs repeated more than once
- Images: store screenshots in `docs/assets/images/screenshots/`, use descriptive alt text, reference with relative paths (e.g., `../assets/images/screenshots/filename.png`)

## File naming

- Lowercase, hyphen-separated: `nonce-tag-helper.md`
- Place in the appropriate section folder

## Process

1. Read existing sibling pages to understand nav_order and context
2. Draft the page content following all conventions
3. Write the file to the correct path under `docs/`
4. Confirm the file was created and the front matter is valid
