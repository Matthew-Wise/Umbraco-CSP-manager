using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Community.CSPManager.Logging;

namespace Umbraco.Community.CSPManager.Notifications.Handlers;

public class CspDistributedCacheRefresher
	: PayloadCacheRefresherBase<CspDistCacheRefresherNotification, CspSavedNotification>
{
	private readonly IAppPolicyCache _runtimeCache;
	private readonly ILogger<CspDistributedCacheRefresher> _logger;

	public CspDistributedCacheRefresher(
			AppCaches appCaches,
			IJsonSerializer serializer,
			ILogger<CspDistributedCacheRefresher> logger,
			IEventAggregator eventAggregator,
			ICacheRefresherNotificationFactory factory
	) : base(appCaches, serializer, eventAggregator, factory)
	{
		_runtimeCache = appCaches.RuntimeCache;
		_logger = logger;
	}
	public static Guid UniqueId => new("8b066ef2-f5ec-4b6f-8121-952c0c7b9d21");
	public override Guid RefresherUniqueId => UniqueId;
	public override string Name => "CspDistCacheRefresher";

	// Deliberately not filtered by server role. Umbraco delivers a refresh instruction to every
	// server, including the one that raised it, and a save can be raised on any of them - a
	// subscriber-only guard meant an instruction arriving at a scheduling publisher was ignored and
	// that server kept serving the old policy. Clearing an already cleared key is a no-op.
	public override void Refresh(CspSavedNotification[] payloads)
	{
		foreach (var payload in payloads)
		{
			var cacheKey = payload.CspDefinition.IsBackOffice
				? Constants.BackOfficeCacheKey
				: Constants.FrontEndCacheKey;
			Log.ClearingCspCache(_logger, cacheKey);
			_runtimeCache.ClearByKey(cacheKey);
		}
	}
	public override void RefreshAll()
	{
		Log.ClearingAllCspCaches(_logger);
		_runtimeCache.ClearByKey(Constants.BackOfficeCacheKey);
		_runtimeCache.ClearByKey(Constants.FrontEndCacheKey);
	}
}