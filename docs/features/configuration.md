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
    "DisableBackOfficeHeader": false,
    "FailureBehavior": "FailOpen"
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

### FailureBehavior

**Type**: `CspFailureBehavior` (`FailOpen` or `FailClosed`)
**Default**: `FailOpen`

Controls what happens if CSP header construction throws, for example when a configured source value is rejected as an invalid header value. Header construction failures never break the request itself - this option only controls what (if anything) is sent in place of the failed header. Either way, the failure is logged as an error.

- `FailOpen` (default): the request continues with no CSP header at all. This matches the package's original behavior - availability is prioritized over security, and the failure is only visible in the logs.
- `FailClosed`: the request continues with a minimal fallback policy, `default-src 'self'`, instead of no header.

```json
{
  "CspManager": {
    "FailureBehavior": "FailClosed"
  }
}
```

{: .warning }
The `FailClosed` fallback is same-origin-only, so it can block cross-origin resources (CDN scripts, fonts, embeds, etc.) until the underlying construction error is fixed - similar to how a misconfigured policy can lock you out of the backoffice. If `FailClosed` breaks the backoffice, use `DisableBackOfficeHeader` as the emergency kill switch above.

See [Troubleshooting](../troubleshooting) for more on diagnosing header construction failures.
