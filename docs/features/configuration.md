---
title: Configuration
parent: Features
nav_order: 2
---

# Configuration

CSP Manager can be configured via `appsettings.json` under the `CspManager` key.

```json
{
  "CspManager": {
    "DisableBackOfficeHeader": false
  }
}
```

## Options

### DisableBackOfficeHeader

**Type**: `bool`
**Default**: `false`

Emergency kill switch to disable the CSP header for the Umbraco backoffice. When set to `true`, no `Content-Security-Policy` or `Content-Security-Policy-Report-Only` header is added to backoffice responses, regardless of what the backoffice policy is configured to do.

Use this if a misconfigured backoffice CSP policy locks you out of the Umbraco admin interface:

```json
{
  "CspManager": {
    "DisableBackOfficeHeader": true
  }
}
```

Remember to set it back to `false` once you have fixed the policy. See [Troubleshooting](../troubleshooting) for more on recovering from a broken backoffice CSP.
