---
title: Importing Policies
parent: User Guide
nav_order: 2
---

# Importing policies

The import feature lets you migrate an existing CSP policy into CSP Manager by pasting a raw policy string. CSP Manager parses the string, groups sources by directive, and replaces your current policy in one step.

{: .warning }
Importing **replaces all existing sources** in the selected policy. Make sure you want to overwrite the current configuration before confirming.

## Opening the import dialog

Right-click a policy node in the CSP Management tree — either **Frontend** or **Backoffice** — and select **Import...** from the action menu.

## Parsing a policy string

Paste your CSP policy value into the textarea and click **Parse**.

{: .note }
Paste the policy *value* only — do not include the header name. Use `default-src 'self'; script-src 'self' https://cdn.example.com` not `Content-Security-Policy: default-src 'self'; ...`.

A preview appears immediately below the textarea.

## Understanding the preview

The preview shows three things:

**Sources** — each unique source value with the directives it applies to. For example, `'self'` with `default-src`, `script-src`, and `style-src` appears as a single row with three directive tags.

**Warnings** — directives that CSP Manager does not recognise are flagged and skipped. Check these to ensure nothing important was lost.

**Special flags** — `upgrade-insecure-requests`, `report-uri`, and `report-to` are treated as policy settings rather than sources. When detected, they appear as flags in the preview and map directly to the corresponding settings in [Policy Settings](policy-settings).

If the preview shows no sources, check that the policy string is correctly formatted and try again.

## Completing the import

Click **Import** to apply the parsed policy. The dialog closes and the workspace updates with the imported sources.

Save your changes in the workspace to persist them.
