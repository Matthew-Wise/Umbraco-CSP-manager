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
    "FailureBehavior": "FailOpen",
    "DisabledNodePolicyBehavior": "FallbackToGlobal"
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

Requests get the backoffice policy when their path is under `/umbraco`, except Umbraco's public areas there: `/umbraco/surface`, `/umbraco/api` and the Delivery API at `/umbraco/delivery/api`. Those, and content URLs that just start with "umbraco" (such as `/umbraco-partners`), get the frontend policy, so this option never removes their header. Plugin controller routes (`/umbraco/{area}/{controller}/{action}`) get the backoffice policy.

### FailureBehavior

**Type**: `CspFailureBehavior` (`FailOpen` or `FailClosed`)
**Default**: `FailOpen`

Controls what happens if CSP header construction throws, for example when a configured source value is rejected as an invalid header value. Header construction failures never break the request itself - this option only controls what (if anything) is sent in place of the failed header. Either way, the failure is logged as an error.

- `FailOpen` (default): the request continues with no CSP header at all. Availability is prioritized over security, and the failure is only visible in the logs.
- `FailClosed`: frontend requests continue with a minimal fallback policy, `default-src 'self'`, instead of no header. Backoffice requests still fail open, so editors can get into the backoffice to fix the policy that failed.

If the CSP definition loaded before the failure and is set to report-only, the fallback is sent as `Content-Security-Policy-Report-Only` too, so a policy you are only trialling is never replaced by an enforced one. If the definition could not be loaded at all (for example, a database error), the fallback is enforced.

```json
{
  "CspManager": {
    "FailureBehavior": "FailClosed"
  }
}
```

{: .warning }
The `FailClosed` fallback is same-origin-only, so it can block cross-origin resources on your site (CDN scripts, fonts, embeds, etc.) until the underlying construction error is fixed.

Both behaviors publish a `CspHeaderConstructionFailedNotification` before the fallback is written, so a handler can set the fallback policy itself for that request — see [Notification Events](../advanced/notification-events#cspheaderconstructionfailednotification).

See [Troubleshooting](../troubleshooting) for more on diagnosing header construction failures.

### DisabledNodePolicyBehavior

**Type**: `FallbackToGlobal` \| `NoHeader`
**Default**: `FallbackToGlobal`

Controls what happens when the [node policy](node-policies) that applies to a request is switched off.

| Value | Behaviour |
|---|---|
| `FallbackToGlobal` | Skips the policy, as if the node had none. The content gets the policy it inherits: the nearest policy further up the content tree, or the Frontend policy. Switching a node policy off retains baseline protection. |
| `NoHeader` | Sends no CSP header for the content a switched-off complete policy applies to, matching the behaviour of a disabled Frontend policy. |

{: .warning }
`NoHeader` is an explicit fail-open setting: switching a complete node policy off removes all CSP protection from its content and, unless it is for that content only, from the pages below it. Keep the default unless you specifically require unconstrained content.

{: .note }
An inherit-and-add policy only adds sources, so switching it off never removes the header: the content keeps the policy it inherits under both values.

```json
{
  "CspManager": {
    "DisabledNodePolicyBehavior": "NoHeader"
  }
}
```

Content without a node policy on it or above it always uses the Frontend policy; this option only applies when a node policy is switched off.
