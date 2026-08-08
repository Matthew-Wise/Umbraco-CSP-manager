using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

internal sealed class CspSavedNotificationHandler : INotificationHandler<CspSavedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;
	private readonly DistributedCache _distributedCache;

	public CspSavedNotificationHandler(
		AppCaches appCaches,
		DistributedCache distributedCache
	)
	{
		_runtimeCache = appCaches.RuntimeCache;
		_distributedCache = distributedCache;
	}

	public void Handle(CspSavedNotification notification)
	{
		string cacheKey = notification.CspDefinition.IsBackOffice ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;

		// Clear locally first so this server serves the new policy immediately, then broadcast to the rest.
		_runtimeCache.ClearByKey(cacheKey);
		_distributedCache.RefreshByPayload(CspDistributedCacheRefresher.UniqueId, [notification]);
	}
}