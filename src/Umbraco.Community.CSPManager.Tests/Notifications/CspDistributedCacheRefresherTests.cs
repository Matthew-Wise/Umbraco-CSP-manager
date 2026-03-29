using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Sync;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;

namespace Umbraco.Community.CSPManager.Tests.Notifications;

[TestFixture]
public class CspDistributedCacheRefresherTests
{
	private Mock<IAppPolicyCache> _runtimeCache;
	private Mock<IServerRoleAccessor> _serverRoleAccessor;
	private CspDistributedCacheRefresher _refresher;

	[SetUp]
	public void SetUp()
	{
		_runtimeCache = new Mock<IAppPolicyCache>();
		_serverRoleAccessor = new Mock<IServerRoleAccessor>();

		var appCaches = new AppCaches(
			_runtimeCache.Object,
			Mock.Of<IRequestCache>(),
			new IsolatedCaches(_ => NoAppCache.Instance));

		_refresher = new CspDistributedCacheRefresher(
			appCaches,
			_serverRoleAccessor.Object,
			Mock.Of<IJsonSerializer>(),
			NullLogger<CspDistributedCacheRefresher>.Instance,
			Mock.Of<IEventAggregator>(),
			Mock.Of<ICacheRefresherNotificationFactory>());
	}

	[Test]
	public void Refresh_AsSubscriber_WithBackOfficePayload_ClearsBackOfficeCache()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);
		var payload = new[] { new CspSavedNotification(new CspDefinition { IsBackOffice = true }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void Refresh_AsSubscriber_WithFrontEndPayload_ClearsFrontEndCache()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);
		var payload = new[] { new CspSavedNotification(new CspDefinition { IsBackOffice = false }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Never);
	}

	[Test]
	public void Refresh_AsNonSubscriber_DoesNotClearCache()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.SchedulingPublisher);
		var payload = new[] { new CspSavedNotification(new CspDefinition { IsBackOffice = true }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(It.IsAny<string>()), Times.Never);
	}

	[Test]
	public void RefreshAll_AsSubscriber_ClearsBothCacheKeys()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);

		_refresher.RefreshAll();

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.DomainCacheKeyPrefix), Times.Once);
	}

	[Test]
	public void RefreshAll_AsNonSubscriber_DoesNotClearCache()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Single);

		_refresher.RefreshAll();

		_runtimeCache.Verify(c => c.ClearByKey(It.IsAny<string>()), Times.Never);
	}

	[Test]
	public void Refresh_AsSubscriber_WithDomainPayload_ClearsDomainCache()
	{
		var domainKey = Guid.NewGuid();
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);
		var payload = new[] { new CspSavedNotification(new CspDefinition { DomainKey = domainKey, IsBackOffice = false }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.DomainCacheKey(domainKey)), Times.Once);
	}

	[Test]
	public void Refresh_AsSubscriber_WithDomainPayload_DoesNotClearGlobalCaches()
	{
		var domainKey = Guid.NewGuid();
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);
		var payload = new[] { new CspSavedNotification(new CspDefinition { DomainKey = domainKey, IsBackOffice = false }) };

		_refresher.Refresh(payload);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Never);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void RefreshAll_AsSubscriber_AlsoClearsDomainCachePrefix()
	{
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);

		_refresher.RefreshAll();

		_runtimeCache.Verify(c => c.ClearByKey(Constants.DomainCacheKeyPrefix), Times.Once);
	}
}
