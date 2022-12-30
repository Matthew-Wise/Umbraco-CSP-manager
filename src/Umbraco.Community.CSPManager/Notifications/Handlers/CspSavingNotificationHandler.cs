namespace Umbraco.Community.CSPManager.Notifications.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;

internal class CspSavingNotificationHandler 
        : INotificationHandler<CspSavingNotification>
{
	private readonly IAppPolicyCache _runtimeCache;

	public CspSavingNotificationHandler(AppCaches appCaches)
	{
		_runtimeCache = appCaches.RuntimeCache;
	}

	public void Handle(CspSavingNotification notification)
	{
		string cacheKey = notification.CspDefinition.IsBackOffice ? CspConstants.BackOfficeCacheKey : CspConstants.FrontEndCacheKey;

		_runtimeCache.ClearByKey(cacheKey);
	}
}
