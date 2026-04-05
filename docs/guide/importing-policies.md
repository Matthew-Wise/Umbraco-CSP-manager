---
title: Importing Policies
parent: User Guide
nav_order: 2
---

# Importing policies

The import feature lets you migrate an existing CSP policy into CSP Manager by pasting a raw policy string. CSP Manager parses the string, groups sources by directive, and saves the result — replacing your current policy in one step.

{: .highlight }
This is the fastest way to migrate from a manually configured header or another CSP tool. Copy the policy value from your existing `Content-Security-Policy` header and paste it directly.

{: .warning }
Importing **replaces all existing sources** in the selected policy and saves immediately. There is no separate save step — the change takes effect as soon as you click **Import**.

## Opening the import dialog

In the CSP Management tree, hover over a policy node — either **Frontend** or **Back Office** — or click the **...** button on the node, then select **Import...**.

## Parsing a policy string

Paste your CSP policy value into the textarea and click **Parse**.

{: .note }
Paste the policy *value* only — do not include the header name. For example:
```
default-src 'self'; script-src 'self' https://cdn.example.com; style-src 'self' 'unsafe-inline';
```
Not:
```
Content-Security-Policy: default-src 'self'; script-src 'self' https://cdn.example.com; ...
```

A preview appears immediately below the textarea.

## Understanding the preview

The preview shows three things:

**Sources** — each unique source value with the directives it applies to. For example, `'self'` appearing in `default-src`, `script-src`, and `style-src` is consolidated into a single row with three directive tags.

**Warnings** — directives that CSP Manager does not recognise are flagged and skipped. Review these to confirm nothing important was lost in the import.

**Special flags** — `upgrade-insecure-requests`, `report-uri`, and `report-to` are treated as policy settings rather than sources. When detected, they appear as flags in the preview and map directly to the corresponding settings in [Policy Settings](policy-settings).

{: .note }
If the preview shows no sources, the **Import** button stays disabled. Check that the policy string contains at least one source directive and try again.

## Completing the import

Click **Import** to apply the parsed policy. CSP Manager saves the change immediately and opens the policy workspace so you can review the result.

To adjust individual sources after importing, use the [Policy Management](policy-management) workspace.
