using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

public class CspDistributedCacheRefresher
	: PayloadCacheRefresherBase<CspDistCacheRefresherNotification, CspSavedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;
	private readonly IServerRoleAccessor _serverRoleAccessor;
	private readonly ILogger<CspDistributedCacheRefresher> _logger;

	public CspDistributedCacheRefresher(
			AppCaches appCaches,
			IServerRoleAccessor serverRoleAccessor,
			IJsonSerializer serializer,
			ILogger<CspDistributedCacheRefresher> logger,
			IEventAggregator eventAggregator,
			ICacheRefresherNotificationFactory factory
	) : base(appCaches, serializer, eventAggregator, factory)
	{
		_runtimeCache = appCaches.RuntimeCache;
		_serverRoleAccessor = serverRoleAccessor;
		_logger = logger;
	}
	public static Guid UniqueId => new("8b066ef2-f5ec-4b6f-8121-952c0c7b9d21");
	public override Guid RefresherUniqueId => UniqueId;
	public override string Name => "CspDistCacheRefresher";
	public override void Refresh(CspSavedNotification[] payloads)
	{
		if (_serverRoleAccessor.CurrentServerRole == ServerRole.Subscriber)
		{
			foreach (var payload in payloads)
			{
				_logger.LogDebug("CSP dist cache refresher. Clearing cache");
				var cacheKey = payload.CspDefinition.IsBackOffice
					? Constants.BackOfficeCacheKey
					: Constants.FrontEndCacheKey;
				_runtimeCache.ClearByKey(cacheKey);
			}
		}
	}
	public override void RefreshAll()
	{
		if (_serverRoleAccessor.CurrentServerRole == ServerRole.Subscriber)
		{
			_logger.LogDebug("CSP dist cache refresher. Clearing all CSP caches");
			_runtimeCache.ClearByKey(Constants.BackOfficeCacheKey);
			_runtimeCache.ClearByKey(Constants.FrontEndCacheKey);
		}
	}
}