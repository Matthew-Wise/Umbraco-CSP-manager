using Umbraco.Cms.Core.Cache;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Tests.Helpers;

namespace Umbraco.Community.CSPManager.Tests.Notifications;

[TestFixture]
public class CspSavedNotificationHandlerTests
{
	private Mock<IAppPolicyCache> _runtimeCache;
	private CspSavedNotificationHandler _handler;
	private SpyServerMessenger _serverMessenger;

	[SetUp]
	public void SetUp()
	{
		_runtimeCache = new Mock<IAppPolicyCache>();
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

		_handler = new CspSavedNotificationHandler(appCaches, distributedCache);
	}

	[Test]
	public void Handle_BackOfficeSave_ClearsBackOfficeCacheKey()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = true });

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void Handle_FrontEndSave_ClearsFrontEndCacheKey()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = false });

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.BackOfficeCacheKey), Times.Never);
	}

	[Test]
	public void Handle_DomainPolicySave_ClearsOnlyThatDomainsCacheKey()
	{
		var contentKey = Guid.NewGuid();
		var notification = new CspSavedNotification(new CspDefinition { Id = Guid.NewGuid(), ContentKey = contentKey });

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.NodeCacheKey(contentKey)), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
	}

	[Test]
	public void Handle_DomainPolicyDelete_ClearsTheDomainCacheKeyAndBroadcasts()
	{
		var contentKey = Guid.NewGuid();
		var notification = new CspDeletedNotification(new CspDefinition { Id = Guid.NewGuid(), ContentKey = contentKey });

		_handler.Handle(notification);

		_runtimeCache.Verify(c => c.ClearByKey(Constants.NodeCacheKey(contentKey)), Times.Once);
		_runtimeCache.Verify(c => c.ClearByKey(Constants.FrontEndCacheKey), Times.Never);
		Assert.That(_serverMessenger.PayloadRefreshCount, Is.EqualTo(1),
			"a delete must invalidate the other servers too");
		var payload = _serverMessenger.LastPayload.Cast<CspSavedNotification>().Single();
		Assert.That(payload.CspDefinition.ContentKey, Is.EqualTo(contentKey),
			"the broadcast payload must identify the deleted node policy's cache entry");
	}

	[Test]
	public void Handle_TriggersDistributedCacheRefresh()
	{
		var notification = new CspSavedNotification(new CspDefinition { IsBackOffice = true });

		_handler.Handle(notification);

		Assert.That(_serverMessenger.PayloadRefreshCount, Is.EqualTo(1));
	}
}
