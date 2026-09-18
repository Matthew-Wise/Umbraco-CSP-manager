---
title: Troubleshooting
nav_order: 7
---

# Troubleshooting

## CSP is blocking legitimate resources

**Symptom**: Resources (images, scripts, fonts, etc.) fail to load and the browser console shows CSP violation errors like `Refused to load the script '...' because it violates the following Content Security Policy directive`.

**Solution**: Switch to report-only mode first so violations are reported without blocking anything:

1. Go to the CSP Manager section in the backoffice
2. Open the affected policy (frontend or backoffice)
3. Enable **Report Only** mode
4. Set a **Report URI** to collect violation reports, or monitor the browser console
5. Identify which sources need to be added, then add them to the policy
6. Once satisfied, switch back to enforcing mode

See [Policy Settings](guide/policy-settings) for details on report-only configuration.

---

## Nonce is not appearing on tags

**Symptom**: The `nonce` attribute is missing from `<script>`, `<style>`, or `<link>` tags even though `csp-manager-add-nonce="true"` is set.

**Common cause**: The tag helper namespace is not registered.

**Solution**: Add the following line to your `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, Umbraco.Community.CSPManager
```

If using a custom view imports file, ensure it is included in the view rendering pipeline.

---

## The backoffice is broken after enabling a strict CSP

**Symptom**: After saving a CSP policy, the Umbraco backoffice stops functioning — JavaScript errors appear and the UI is unresponsive.

**Cause**: The backoffice policy has directives that are too restrictive, blocking Umbraco's own scripts or styles.

**Solutions**:

1. **Use the emergency kill switch** — Set `DisableBackOfficeHeader` to `true` in `appsettings.json` to immediately stop CSP headers being sent for the backoffice:

   ```json
   {
     "CspManager": {
       "DisableBackOfficeHeader": true
     }
   }
   ```

   This allows you to regain access to the backoffice and fix the policy. Remember to set it back to `false` once fixed.

2. **Switch the backoffice policy to report-only** before making it stricter, so you can identify any violations without breaking the UI.

See [Configuration](features/configuration) for more on `DisableBackOfficeHeader`.

---

## How to read CSP violation messages in the browser

Open your browser's developer tools (F12) and go to the **Console** tab. CSP violations appear as errors starting with `Refused to ...`.

The message typically contains:
- The resource URL that was blocked
- The directive that blocked it (e.g., `script-src`, `style-src`)
- The policy source that would need to be added

Example:
```
Refused to load the script 'https://cdn.example.com/lib.js' because it violates
the following Content Security Policy directive: "script-src 'self'".
```

This tells you to add `https://cdn.example.com` as a source for the `script-src` directive.

---

## CSP headers are not being sent at all

**Possible causes**:

- The policy is **disabled** — check the Enabled toggle in [Policy Settings](guide/policy-settings)
- `DisableBackOfficeHeader` is `true` for the backoffice policy — check `appsettings.json`
- Umbraco has not yet reached `RuntimeLevel.Run` (e.g., during installation or upgrade) — this is expected behaviour; the middleware waits for Umbraco to be fully running
- Header construction **threw an exception** and `FailureBehavior` is left at its default, `FailOpen` — check your logs for a `Failed to construct CSP header for {Path}` error. Set `FailureBehavior` to `FailClosed` in `appsettings.json` to apply a minimal `default-src 'self'` fallback policy instead of no header while you fix the underlying error. See [Configuration](features/configuration) for details.

---

## Getting further help

- [GitHub Issues](https://github.com/Matthew-Wise/Umbraco-CSP-manager/issues) — search for existing issues or open a new one
- [GitHub Discussions](https://github.com/Matthew-Wise/Umbraco-CSP-manager/discussions) — ask questions or share ideas
- [Umbraco Community Forum](https://our.umbraco.com) — for general Umbraco questions
