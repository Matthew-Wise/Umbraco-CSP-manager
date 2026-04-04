using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Sync;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Tests.Helpers;

namespace Umbraco.Community.CSPManager.Tests.Notifications;

[TestFixture]
public class CspSavedNotificationHandlerTests
{
	private Mock<IAppPolicyCache> _runtimeCache;
	private Mock<IServerRoleAccessor> _serverRoleAccessor;
	private CspSavedNotificationHandler _handler;
	private SpyServerMessenger _serverMessenger;

	[SetUp]
	public void SetUp()
	{
		_runtimeCache = new Mock<IAppPolicyCache>();
		_serverRoleAccessor = new Mock<IServerRoleAccessor>();
		_serverMessenger = new SpyServerMessenger();

		var cacheRefresherCollection = new CacheRefresherCollection(() => new ICacheRefresher[]
		{
			new StubCacheRefresher()
		});
		var distributedCache = new DistributedCache(_serverMessenger, cacheRefresherCollection);

		var appCaches = new AppCaches(
			_runtimeCache.Object,
			Mock.Of<IRequestCache>(),
			new IsolatedCaches(_ => NoAppCache.Instance));

		_handler = new CspSavedNotificationHandler(_serverRoleAccessor.Object, appCaches, distributedCache);
	}

	[Test]
	public void Handle_BackOfficeSave_ClearsBackOfficeCacheKey()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = true });
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Single);

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void Handle_FrontEndSave_ClearsFrontEndCacheKey()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = false });
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Single);

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Never);
	}

	[Test]
	public void Handle_WhenSchedulingPublisher_TriggersDistributedCacheRefresh()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = true });
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.SchedulingPublisher);

		_handler.Handle(notification);

		Assert.That(_serverMessenger.PayloadRefreshCount, Is.EqualTo(1));
	}

	[Test]
	public void Handle_WhenNotSchedulingPublisher_DoesNotTriggerDistributedCacheRefresh()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = true });
		_serverRoleAccessor.Setup(x => x.CurrentServerRole).Returns(ServerRole.Subscriber);

		_handler.Handle(notification);

		Assert.That(_serverMessenger.PayloadRefreshCount, Is.Zero);
	}

}