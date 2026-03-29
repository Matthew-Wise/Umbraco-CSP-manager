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
		string cacheKey = GetCacheKey(notification.CspDefinition);
		_runtimeCache.ClearByKey(cacheKey);

		if (_serverRoleAccessor.CurrentServerRole == ServerRole.SchedulingPublisher)
		{
			_distributedCache.RefreshByPayload(CspDistributedCacheRefresher.UniqueId, [notification]);
		}
	}

	private static string GetCacheKey(Models.CspDefinition definition)
	{
		if (definition.DomainKey.HasValue)
		{
			return Constants.DomainCacheKey(definition.DomainKey.Value);
		}

		return definition.IsBackOffice ? Constants.BackOfficeCacheKey : Constants.FrontEndCacheKey;
	}
}