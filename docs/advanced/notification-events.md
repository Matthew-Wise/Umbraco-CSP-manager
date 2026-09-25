---
title: Notification Events
parent: Advanced
nav_order: 1
---

# Notification Events

CSP Manager raises Umbraco notification events that allow you to extend its behaviour and integrate with your application logic.

## CspWritingNotification

Raised when the middleware is building a CSP definition for an HTTP request, before the header is written to the response. Use this to dynamically modify the CSP based on request context.

**Properties**:
- `CspDefinition` — the current CSP definition being applied (may be `null`)
- `HttpContext` — the current HTTP context

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;

public class CustomCspWritingHandler : INotificationHandler<CspWritingNotification>
{
    public void Handle(CspWritingNotification notification)
    {
        if (notification.CspDefinition is null) return;

        // Add an extra source when serving API requests
        if (notification.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            notification.CspDefinition.Sources.Add(new CspDefinitionSource
            {
                DefinitionId = notification.CspDefinition.Id,
                Source = "api.example.com",
                Directives = ["connect-src"],
            });
        }
    }
}
```

## CspHeaderConstructionFailedNotification

Raised when building the CSP header for a request throws — for example when a source value is rejected as an invalid header value. Use this to set the fallback policy yourself, per request, instead of relying on the configured [`FailureBehavior`](../features/configuration#failurebehavior) alone.

**Properties**:
- `Exception` — the exception thrown while constructing the header (read-only)
- `HttpContext` — the current HTTP context (read-only)
- `IsBackOfficeRequest` — whether the failed header was for a backoffice request (read-only)
- `FallbackPolicy` — the policy sent in place of the failed header. Pre-populated from `FailureBehavior`: `null` for `FailOpen`, `default-src 'self'` for `FailClosed` on frontend requests. Backoffice requests always start from `null`. Set it to `null` to send no header at all
- `ReportOnly` — send `FallbackPolicy` as a report-only header rather than an enforced one. Pre-populated from the CSP definition when it loaded before the failure; otherwise `false`

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;

public class CustomCspFallbackHandler : INotificationHandler<CspHeaderConstructionFailedNotification>
{
    public void Handle(CspHeaderConstructionFailedNotification notification)
    {
        // Keep the backoffice usable, lock the front end down to a known-good policy
        if (notification.IsBackOfficeRequest)
        {
            notification.FallbackPolicy = null;
            return;
        }

        notification.FallbackPolicy = "default-src 'self';img-src 'self' data:";

        // AlertSecurityTeam(notification.Exception);
    }
}
```

{: .note }
The handler has the final say — whatever it leaves in `FallbackPolicy` is what gets sent, whichever `FailureBehavior` is configured. If a handler throws, the failure is logged and the configured `FailureBehavior` fallback is applied instead; either way the request itself still completes.

## CspSavedNotification

Raised when a CSP definition is saved through the backoffice. Use this for cache invalidation, logging, or integration with external systems.

**Properties**:
- `CspDefinition` — the saved CSP definition

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;

public class CustomCspSavedHandler : INotificationHandler<CspSavedNotification>
{
    public void Handle(CspSavedNotification notification)
    {
        var csp = notification.CspDefinition;
        // Log CSP changes
        _logger.LogInformation("CSP policy updated for {Area}",
            csp.IsBackOffice ? "BackOffice" : "Frontend");

        // Integrate with external monitoring
        // NotifySecurityTeam(csp);
    }
}
```

## Cache

CSP Manager caches policies and automatically clears the cache when a policy is saved — including across all servers in a load-balanced environment. No additional configuration is required.

`CspDistCacheRefresherNotification` is raised when the cache is cleared. You can handle it if you need to react to these events, but in most cases you won't need to:

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Community.CSPManager.Notifications;

public class MyCacheRefreshHandler : INotificationHandler<CspDistCacheRefresherNotification>
{
    public void Handle(CspDistCacheRefresherNotification notification)
    {
        _logger.LogInformation("CSP cache refreshed on this node");
    }
}
```

## Registering Handlers

Register your custom handlers using Umbraco's composer pattern:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<CspWritingNotification, CustomCspWritingHandler>();
        builder.AddNotificationHandler<CspSavedNotification, CustomCspSavedHandler>();
        builder.AddNotificationHandler<CspHeaderConstructionFailedNotification, CustomCspFallbackHandler>();
    }
}
```
