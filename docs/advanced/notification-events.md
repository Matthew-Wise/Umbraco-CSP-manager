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
    }
}
```
