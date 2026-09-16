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

**Type**: `FailOpen` \| `FailClosed`
**Default**: `FailOpen`

Controls what happens if constructing the CSP header throws an unexpected exception (for example, a source value Kestrel rejects as an invalid header). This should be rare in practice, but decides what a request gets when it does happen.

- `FailOpen` (default): the request continues with **no** `Content-Security-Policy` header. Availability is prioritized over security; the failure is only visible in the logs (`CspHeaderConstructionFailed`).
- `FailClosed`: the request continues with a minimal fallback policy, `default-src 'self'`, instead of no header at all. A `CspFailClosedFallbackApplied` warning is also logged.

```json
{
  "CspManager": {
    "FailureBehavior": "FailClosed"
  }
}
```

{: .note }
`FailClosed` applies to the same-origin fallback policy, not the policy you configured. If your backoffice or frontend relies on cross-origin sources (CDNs, embedded fonts, third-party scripts) and header construction fails, those requests will be blocked by the fallback until the underlying error is fixed. Use [`DisableBackOfficeHeader`](#disablebackofficeheader) as the emergency kill switch if the fallback locks you out of the backoffice.
