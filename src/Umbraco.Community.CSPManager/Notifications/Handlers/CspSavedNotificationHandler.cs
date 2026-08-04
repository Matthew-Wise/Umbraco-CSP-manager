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

		// Clear locally first so this server serves the new policy immediately whatever its role,
		// and regardless of how the messenger is configured.
		_runtimeCache.ClearByKey(cacheKey);

		// Then tell the other servers. This is not limited to the scheduling publisher: a back
		// office save is served by whichever server the editor happens to be on, so restricting the
		// broadcast to publishers left every other server serving the old policy until it recycled.
		_distributedCache.RefreshByPayload(CspDistributedCacheRefresher.UniqueId, [notification]);
	}
}