using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;

namespace Umbraco.Community.CSPManager.Tests.Notifications;

[TestFixture]
public class CspDistributedCacheRefresherTests
{
	private Mock<IAppPolicyCache> _runtimeCache;
	private CspDistributedCacheRefresher _refresher;

	[SetUp]
	public void SetUp()
	{
		_runtimeCache = new Mock<IAppPolicyCache>();

		var appCaches = new AppCaches(
			_runtimeCache.Object,
			Mock.Of<IRequestCache>(),
			new IsolatedCaches(_ => NoAppCache.Instance));

		_refresher = new CspDistributedCacheRefresher(
			appCaches,
			Mock.Of<IJsonSerializer>(),
			NullLogger<CspDistributedCacheRefresher>.Instance,
			Mock.Of<IEventAggregator>(),
			Mock.Of<ICacheRefresherNotificationFactory>());
	}

	[Test]
	public void Refresh_WithBackOfficePayload_ClearsBackOfficeCache()
	{
		var payload = new[] { new CspSavedNotification(new CspDefinition { IsBackOffice = true }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void Refresh_WithFrontEndPayload_ClearsFrontEndCache()
	{
		var payload = new[] { new CspSavedNotification(new CspDefinition { IsBackOffice = false }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Never);
	}

	[Test]
	public void Refresh_WithPayloadsForBothContexts_ClearsBothCacheKeys()
	{
		var payload = new[]
		{
			new CspSavedNotification(new CspDefinition { IsBackOffice = true }),
			new CspSavedNotification(new CspDefinition { IsBackOffice = false })
		};

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
	}

	[Test]
	public void RefreshAll_ClearsBothCacheKeys()
	{
		_refresher.RefreshAll();

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
	}
}
