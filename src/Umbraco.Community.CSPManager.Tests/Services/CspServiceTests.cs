using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.CSPManager.Migrations;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Services;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CspServiceTests : UmbracoIntegrationTest
{
	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.AddComposers();
	}

	private ICspService _cspService;

	[SetUp]
	public async Task SetUp()
	{
		var upgrader = new Upgrader(new CspMigrationPlan());
		var result = await upgrader.ExecuteAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>()).ConfigureAwait(false);
		if (!result.Successful)
		{
			TestContext.WriteLine(result.Exception.Message);
		}
		Assert.That(result.Successful, Is.True);
		_cspService = GetRequiredService<ICspService>();
	}

	[Test]
	public void GetCspDefinition_WhenNoDefinitionExists_ReturnsDefaultDefinition()
	{
		var result = _cspService.GetCspDefinition(isBackOfficeRequest: false);

		Assert.That(result, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(result.Enabled, Is.False);
			Assert.That(result.IsBackOffice, Is.False);
		});
	}

	[Test]
	public void GetCspDefinition_BackOffice_WhenNoDefinitionExists_ReturnsDefaultBackOfficeDefinition()
	{
		var result = _cspService.GetCspDefinition(isBackOfficeRequest: true);

		Assert.That(result, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(result.Id, Is.EqualTo(Constants.DefaultBackofficeId));
			Assert.That(result.Enabled, Is.False);
			Assert.That(result.IsBackOffice, Is.True);
		});
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

		await _cspService.SaveCspDefinitionAsync(originalDefinition);

		originalDefinition.Enabled = true;
		originalDefinition.Sources.Add(new CspDefinitionSource
		{
			DefinitionId = originalDefinition.Id,
			Source = "example.com",
			Directives = [Constants.Directives.ScriptSource]
		});

		var result = await _cspService.SaveCspDefinitionAsync(originalDefinition);

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

		var result = await _cspService.SaveCspDefinitionAsync(definition);

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

		var serviceWithMockEventAggregator = new CspService(mockEventAggregator, ScopeProvider, AppCaches.RuntimeCache);

		var definition = new CspDefinition
		{
			Id = Guid.NewGuid(),
			Enabled = true,
			IsBackOffice = false,
			Sources = []
		};

		await serviceWithMockEventAggregator.SaveCspDefinitionAsync(definition);

		Assert.That(notificationPublished, Is.True);
	}

	[Test]
	public void GetCspScriptNonce_WithValidContext_ReturnsNonce()
	{
		var context = new DefaultHttpContext();

		var nonce1 = _cspService.GetOrCreateCspScriptNonce(context);
		var nonce2 = _cspService.GetOrCreateCspScriptNonce(context);

		Assert.Multiple(() =>
		{
			Assert.That(nonce1, Is.Not.Null.And.Not.Empty);
			Assert.That(nonce2, Is.EqualTo(nonce1));
			Assert.That(nonce1, Has.Length.EqualTo(24));
		});
	}

	[Test]
	public void GetCspStyleNonce_WithValidContext_ReturnsNonce()
	{
		var context = new DefaultHttpContext();

		var nonce1 = _cspService.GetOrCreateCspStyleNonce(context);
		var nonce2 = _cspService.GetOrCreateCspStyleNonce(context);

		Assert.Multiple(() =>
		{
			Assert.That(nonce1, Is.Not.Null.And.Not.Empty);
			Assert.That(nonce2, Is.EqualTo(nonce1));
			Assert.That(nonce1, Has.Length.EqualTo(24));
		});
	}

	[Test]
	public void GetCspScriptNonce_AndGetCspStyleNonce_ReturnDifferentNonces()
	{
		var context = new DefaultHttpContext();

		var scriptNonce = _cspService.GetOrCreateCspScriptNonce(context);
		var styleNonce = _cspService.GetOrCreateCspStyleNonce(context);

		Assert.That(scriptNonce, Is.Not.EqualTo(styleNonce));
	}

	[Test]
	public void GetCachedCspDefinition_CachesResult()
	{
		// Create a spy cache that counts factory calls
		var httpContextAccessor = GetRequiredService<IHttpContextAccessor>();
		var requestCache = new HttpContextRequestAppCache(httpContextAccessor);
		var realCache = AppCaches.Create(requestCache).RuntimeCache;
		var factoryCallCount = 0;
		var spyCache = Mock.Of<IAppPolicyCache>();
		
		Mock.Get(spyCache)
			.Setup(x => x.Get(It.IsAny<string>(), It.IsAny<Func<object>>()))
			.Returns((string key, Func<object> factory) =>
			{
				// Use real cache but count factory calls
				return realCache.GetCacheItem(key, () =>
				{
					factoryCallCount++;
					return factory();
				});
			});

		var spyService = new CspService(GetRequiredService<IEventAggregator>(), ScopeProvider, spyCache);

		// Call GetCachedCspDefinition twice
		var definition1 = spyService.GetCachedCspDefinition(isBackOfficeRequest: true);
		var definition2 = spyService.GetCachedCspDefinition(isBackOfficeRequest: true);

		// Factory should only be called once (first call), second should come from cache
		Assert.Multiple(() =>
		{
			Assert.That(definition1, Is.Not.Null);
			Assert.That(definition2, Is.Not.Null);
			Assert.That(factoryCallCount, Is.EqualTo(1), "Factory should only be called once - second call should use cache");
		});
	}

	[Test]
	public void GetCachedCspDefinition_CachesSeparatelyForBackOfficeAndFrontEnd()
	{
		var frontEndDefinition = _cspService.GetCachedCspDefinition(isBackOfficeRequest: false);
		var backOfficeDefinition = _cspService.GetCachedCspDefinition(isBackOfficeRequest: true);

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

		await _cspService.SaveCspDefinitionAsync(definition);

		var retrievedDefinition = _cspService.GetCspDefinition(isBackOfficeRequest: false);

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