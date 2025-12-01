using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

internal sealed class CspSavedNotificationHandler : INotificationHandler<CspSavedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;
	private readonly IServerRoleAccessor _serverRoleAccessor;
	private readonly DistributedCache _distributedCache;

	public CspSavedNotificationHandler(
		IServerRoleAccessor serverRoleAccessor,
		AppCaches appCaches,
		DistributedCache distributedCache
	)
	{
		_serverRoleAccessor = serverRoleAccessor;
		_runtimeCache = appCaches.RuntimeCache;
		_distributedCache = distributedCache;
	}

	public void Handle(CspSavedNotification notification)
	{
		string cacheKey = notification.CspDefinition.IsBackOffice ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
		_runtimeCache.ClearByKey(cacheKey);


		if (_serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher)
			_distributedCache.RefreshByPayload(CspDistributedCacheRefresher.UniqueId, [notification]);
	}
}