namespace Umbraco.Community.CSPManager.Notifications.Handlers;

using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;

internal sealed class CspSavedNotificationHandler : INotificationHandler<CspSavedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;

	public CspSavedNotificationHandler(AppCaches appCaches)
	{
		_runtimeCache = appCaches.RuntimeCache;
	}

	public void Handle(CspSavedNotification notification)
	{
		string cacheKey = notification.CspDefinition.IsBackOffice ? CspConstants.BackOfficeCacheKey : CspConstants.FrontEndCacheKey;

		_runtimeCache.ClearByKey(cacheKey);
	}
}
