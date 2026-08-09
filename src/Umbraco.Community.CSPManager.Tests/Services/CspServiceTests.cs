using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Tests.Helpers;

namespace Umbraco.Community.CSPManager.Tests.Services;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CspServiceTests : UmbracoIntegrationTest
{
	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.AddComposers();
	}

	protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
	{
		base.SetUpTestConfiguration(configBuilder);
		// Umbraco 17.3 runs package migrations via a background service when PackageMigrationsUnattended=true,
		// which conflicts with the manual migration execution in SetUp. Disable it so tests manage their own migrations.
		configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["Umbraco:CMS:Unattended:PackageMigrationsUnattended"] = "false"
		});
	}

	private ICspService _cspService;

	[SetUp]
	public async Task SetUp()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());
		_cspService = GetRequiredService<ICspService>();
	}

	[Test]
	public async Task GetCspDefinitionAsync_WhenNoDefinitionExists_ReturnsDefaultDefinition()
	{
		var result = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.That(result, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(result.Enabled, Is.False);
			Assert.That(result.IsBackOffice, Is.False);
		});
	}

	[Test]
	public async Task GetCspDefinitionAsync_BackOffice_WhenNoDefinitionExists_ReturnsDefaultBackOfficeDefinition()
	{
		var result = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);

		Assert.That(result, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(Constants.DefaultBackofficeId));
			Assert.That(result.Enabled, Is.False);
			Assert.That(result.IsBackOffice, Is.True);
		});
	}

	[Test]
	public async Task GetCspDefinitionAsync_ByKey_WhenDefinitionExists_ReturnsDefinitionWithSources()
	{
		var id = Guid.NewGuid();
		var definition = new CspDefinition
		{
			Id = id,
			Enabled = true,
			IsBackOffice = false,
			Sources =
			[
				new() { DefinitionId = id, Source = "'self'", Directives = [Constants.Directives.DefaultSource] }
			]
		};
		await _cspService.SaveCspDefinitionAsync(definition, CancellationToken.None);

		var result = await _cspService.GetCspDefinitionAsync(id, CancellationToken.None);

		Assert.That(result, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(id));
			Assert.That(result.Enabled, Is.True);
			Assert.That(result.Sources, Has.Count.EqualTo(1));
			Assert.That(result.Sources[0].Source, Is.EqualTo("'self'"));
		});
	}

	[Test]
	public async Task GetCspDefinitionAsync_ByKey_WhenDefinitionDoesNotExist_ReturnsNull()
	{
		var result = await _cspService.GetCspDefinitionAsync(Guid.NewGuid(), CancellationToken.None);

		Assert.That(result, Is.Null);
	}

	[Test]
	public async Task SaveCspDefinitionAsync_UpdatesExistingDefinition()
	{
		var originalDefinition = new CspDefinition
		{
			Id = Guid.NewGuid(),
			Enabled = false,
			IsBackOffice = true,
			Sources = []
		};

		await _cspService.SaveCspDefinitionAsync(originalDefinition, CancellationToken.None);

		originalDefinition.Enabled = true;
		originalDefinition.Sources.Add(new CspDefinitionSource
		{
			DefinitionId = originalDefinition.Id,
			Source = "example.com",
			Directives = [Constants.Directives.ScriptSource]
		});

		var result = await _cspService.SaveCspDefinitionAsync(originalDefinition, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(result.Enabled, Is.True);
			Assert.That(result.Sources, Has.Count.EqualTo(1));
		});
		Assert.That(result.Sources.FirstOrDefault().Source, Is.EqualTo("example.com"));
	}

	[Test]
	public async Task SaveCspDefinitionAsync_RemovesEmptySources()
	{
		var definition = new CspDefinition
		{
			Id = Constants.DefaultBackofficeId,
			Enabled = true,
			IsBackOffice = false,
			Sources =
		   [
			   new()
			   {
				   DefinitionId = Constants.DefaultBackofficeId,
				   Source = "'self'",
				   Directives = [Constants.Directives.DefaultSource]
			   },
			   new()
			   {
				   DefinitionId = Constants.DefaultBackofficeId,
				   Source = "",
				   Directives = [Constants.Directives.ScriptSource]
			   },
			   new()
			   {
				   DefinitionId = Constants.DefaultBackofficeId,
				   Source = "   ",
				   Directives = [Constants.Directives.StyleSource]
			   }
		   ]
		};

		var result = await _cspService.SaveCspDefinitionAsync(definition, CancellationToken.None);

		Assert.That(result.Sources, Has.Count.EqualTo(1));
		Assert.That(result.Sources.FirstOrDefault().Source, Is.EqualTo("'self'"));
	}

	[Test]
	public async Task SaveCspDefinitionAsync_PublishesCspSavedNotification()
	{
		var notificationPublished = false;
		var mockEventAggregator = Mock.Of<IEventAggregator>();
		Mock.Get(mockEventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspSavedNotification>(), It.IsAny<CancellationToken>()))
			.Callback<INotification, CancellationToken>((notification, _) => notificationPublished = true)
			.Returns(Task.CompletedTask);

		var serviceWithMockEventAggregator = new CspService(mockEventAggregator, ScopeProvider, AppCaches, NullLogger<CspService>.Instance);

		var definition = new CspDefinition
		{
			Id = Guid.NewGuid(),
			Enabled = true,
			IsBackOffice = false,
			Sources = []
		};

		await serviceWithMockEventAggregator.SaveCspDefinitionAsync(definition, CancellationToken.None);

		Assert.That(notificationPublished, Is.True);
	}

	[Test]
	public void GetOrCreateCspNonce_WithValidContext_ReturnsNonce()
	{
		var context = new DefaultHttpContext();

		var nonce1 = _cspService.GetOrCreateCspNonce(context);
		var nonce2 = _cspService.GetOrCreateCspNonce(context);

		Assert.Multiple(() =>
		{
			Assert.That(nonce1, Is.Not.Null.And.Not.Empty);
			Assert.That(nonce2, Is.EqualTo(nonce1));
			Assert.That(nonce1, Has.Length.EqualTo(24));
		});
	}

	[Test]
	public async Task GetCachedCspDefinitionAsync_CachesResult()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = new CspService(GetRequiredService<IEventAggregator>(), ScopeProvider, caches, NullLogger<CspService>.Instance);

		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultBackofficeId,
			Enabled = true,
			IsBackOffice = true,
			Sources = []
		}, CancellationToken.None);

		var definition1 = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);
		Assert.That(definition1.Enabled, Is.True);

		// Saved through a different CspService/cache instance (_cspService uses the DI-registered
		// AppCaches), so `service`'s own cache is never invalidated by this - the row it points at
		// changes underneath it.
		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultBackofficeId,
			Enabled = false,
			IsBackOffice = true,
			Sources = []
		}, CancellationToken.None);

		var definition2 = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);

		Assert.That(definition2.Enabled, Is.True,
			"the second call should be served from cache, not reloaded from the database");
	}

	[Test]
	public async Task GetCachedCspDefinitionAsync_ReturnsIndependentCopyPerCall()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = new CspService(GetRequiredService<IEventAggregator>(), ScopeProvider, caches, NullLogger<CspService>.Instance);

		var definition1 = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		definition1.Enabled = true;
		definition1.Sources.Add(new CspDefinitionSource
		{
			DefinitionId = definition1.Id,
			Source = "mutated-by-caller",
			Directives = [Constants.Directives.ConnectSource]
		});

		var definition2 = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		// A caller (e.g. a CspWritingNotification handler following the documented pattern of
		// mutating notification.CspDefinition) must not be able to corrupt what every other
		// request sharing the cache sees.
		Assert.Multiple(() =>
		{
			Assert.That(definition2, Is.Not.SameAs(definition1));
			Assert.That(definition2.Enabled, Is.False);
			Assert.That(definition2.Sources.Select(s => s.Source), Does.Not.Contain("mutated-by-caller"));
		});
	}

	[Test]
	public async Task GetCachedCspDefinitionAsync_CachesTheLoadBeforeAwaitingIt()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = new CspService(GetRequiredService<IEventAggregator>(), ScopeProvider, caches, NullLogger<CspService>.Instance);

		// Not awaited yet: everything up to the first await has run, including the cache insert.
		var pending = service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.That(caches.RuntimeCache.Get(Constants.FrontEndCacheKey), Is.Not.Null,
			"the in-flight load should already be cached");

		await pending;
	}

	[Test]
	public async Task GetCachedCspDefinitionAsync_WhenClearedMidLoad_DoesNotReCacheTheStaleLoad()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = new CspService(GetRequiredService<IEventAggregator>(), ScopeProvider, caches, NullLogger<CspService>.Instance);

		var pending = service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		// A save landing while the load is in flight.
		caches.RuntimeCache.ClearByKey(Constants.FrontEndCacheKey);

		await pending;

		Assert.That(caches.RuntimeCache.Get(Constants.FrontEndCacheKey), Is.Null,
			"a load that started before the invalidation must not repopulate the cache");
	}

	// Wires the service and saved-notification handler up against a shared real cache to
	// exercise the invalidation end to end.
	[Test]
	public async Task SaveCspDefinitionAsync_InvalidatesTheCachedDefinition()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var distributedCache = new DistributedCache(
			new SpyServerMessenger(),
			new CacheRefresherCollection(() => new ICacheRefresher[] { new StubCacheRefresher() }));
		var handler = new CspSavedNotificationHandler(caches, distributedCache);

		var eventAggregator = Mock.Of<IEventAggregator>();
		Mock.Get(eventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspSavedNotification>(), It.IsAny<CancellationToken>()))
			.Callback<CspSavedNotification, CancellationToken>((notification, _) => handler.Handle(notification))
			.Returns(Task.CompletedTask);

		var service = new CspService(eventAggregator, ScopeProvider, caches, NullLogger<CspService>.Instance);

		await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		Assert.That(caches.RuntimeCache.Get(Constants.FrontEndCacheKey), Is.Not.Null);

		await service.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			IsBackOffice = false,
			Sources =
			[
				new()
				{
					DefinitionId = Constants.DefaultFrontEndId,
					Source = "'self'",
					Directives = [Constants.Directives.DefaultSource]
				}
			]
		}, CancellationToken.None);

		Assert.That(caches.RuntimeCache.Get(Constants.FrontEndCacheKey), Is.Null,
			"saving should invalidate the cached definition");

		var reloaded = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.That(reloaded, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(reloaded.Enabled, Is.True);
			Assert.That(reloaded.Sources, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task GetCachedCspDefinitionAsync_CachesSeparatelyForBackOfficeAndFrontEnd()
	{
		var frontEndDefinition = await _cspService.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		var backOfficeDefinition = await _cspService.GetCachedCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(frontEndDefinition.IsBackOffice, Is.False);
			Assert.That(backOfficeDefinition.IsBackOffice, Is.True);
			Assert.That(frontEndDefinition.Id, Is.Not.EqualTo(backOfficeDefinition.Id));
		});
	}

	[Test]
	public async Task Integration_SaveAndRetrieveDefinition()
	{
		var definitionId = Guid.NewGuid();
		var definition = new CspDefinition
		{
			Id = definitionId,
			Enabled = true,
			IsBackOffice = false,
			ReportOnly = true,
			ReportUri = "https://example.com/csp-report",
			Sources =
		   [
			   new()
			   {
				   DefinitionId = definitionId,
				   Source = "'self'",
				   Directives = [Constants.Directives.DefaultSource]
			   },
			   new()
			   {
				   DefinitionId = definitionId,
				   Source = "https://cdn.example.com",
				   Directives = [Constants.Directives.ScriptSource, Constants.Directives.StyleSource]
			   }
		   ]
		};

		await _cspService.SaveCspDefinitionAsync(definition, CancellationToken.None);

		var retrievedDefinition = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.That(retrievedDefinition, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(retrievedDefinition.Id, Is.EqualTo(definitionId));
			Assert.That(retrievedDefinition.Enabled, Is.True);
			Assert.That(retrievedDefinition.ReportOnly, Is.True);
			Assert.That(retrievedDefinition.ReportUri, Is.EqualTo("https://example.com/csp-report"));
			Assert.That(retrievedDefinition.Sources, Has.Count.EqualTo(2));
		});

		var selfSource = retrievedDefinition.Sources.First(s => s.Source == "'self'");
		Assert.That(selfSource.Directives, Contains.Item(Constants.Directives.DefaultSource));

		var cdnSource = retrievedDefinition.Sources.First(s => s.Source == "https://cdn.example.com");
		Assert.That(cdnSource.Directives, Contains.Item(Constants.Directives.ScriptSource));
		Assert.That(cdnSource.Directives, Contains.Item(Constants.Directives.StyleSource));
	}
}