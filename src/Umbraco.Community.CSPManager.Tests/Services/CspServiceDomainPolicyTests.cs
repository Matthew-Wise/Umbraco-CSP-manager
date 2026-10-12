using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models;
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
public class CspServiceDomainPolicyTests : UmbracoIntegrationTestWithContent
{
	private ICspService _cspService;
	private IDomainService _domainService;
	// Node policies belong to content nodes: A and B have a hostname each, the third only a
	// culture (wildcard) domain, which never qualifies.
	private Guid _nodeA;
	private Guid _nodeB;
	private Guid _wildcardOnlyNode;

	protected override void CustomTestSetup(IUmbracoBuilder builder)
	{
		builder.AddComposers();
	}

	protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
	{
		base.SetUpTestConfiguration(configBuilder);
		// Tests run the package migrations themselves (see CspServiceTests).
		configBuilder.AddInMemoryCollection(new Dictionary<string, string>
		{
			["Umbraco:CMS:Unattended:PackageMigrationsUnattended"] = "false"
		});
	}

	[SetUp]
	public async Task SetUpDomains()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());
		_cspService = GetRequiredService<ICspService>();
		_domainService = GetRequiredService<IDomainService>();

		await CspTestDomainHelper.AssignDomainsAsync(_domainService, Textpage.Key, ["a.example.com"]);
		_nodeA = Textpage.Key;
		await CspTestDomainHelper.AssignDomainsAsync(_domainService, Subpage2.Key, ["b.example.com"]);
		_nodeB = Subpage2.Key;

		var subpageDomains = await CspTestDomainHelper.AssignDomainsAsync(_domainService, Subpage.Key, [], defaultIsoCode: "en-US");
		Assert.That(subpageDomains.Single().IsWildcard, Is.True);
		_wildcardOnlyNode = Subpage.Key;
	}

	private CspService CreateService(AppCaches caches, IEventAggregator eventAggregator = null)
		=> new(eventAggregator ?? GetRequiredService<IEventAggregator>(), ScopeProvider, caches, NullLogger<CspService>.Instance, _domainService, GetRequiredService<IEntityService>());

	private static CspDefinition NewDomainPolicy(Guid contentKey, Guid? id = null, params string[] sources)
	{
		var definitionId = id ?? Guid.Empty;
		return new CspDefinition
		{
			Id = definitionId,
			ContentKey = contentKey,
			Enabled = true,
			Sources = [.. sources.Select(s => new CspDefinitionSource { DefinitionId = definitionId, Source = s, Directives = [Constants.Directives.DefaultSource] })]
		};
	}

	private async Task SaveFrontendAsync(params string[] sources)
		=> await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			Sources = [.. sources.Select(s => new CspDefinitionSource { DefinitionId = Constants.DefaultFrontEndId, Source = s, Directives = [Constants.Directives.DefaultSource] })]
		}, CancellationToken.None);

	// ── Create ───────────────────────────────────────────────────────────────

	[Test]
	public async Task CreateCspDefinitionForDomainAsync_CopiesTheFrontendPolicy()
	{
		await SaveFrontendAsync("'self'", "cdn.example.com");

		var created = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionAsync(created.Id, CancellationToken.None);
		Assert.That(stored, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(stored.Id, Is.Not.EqualTo(Guid.Empty).And.Not.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(stored.ContentKey, Is.EqualTo(_nodeA));
			Assert.That(stored.IsBackOffice, Is.False);
			Assert.That(stored.Enabled, Is.True);
			Assert.That(stored.Sources.Select(s => s.Source), Is.EquivalentTo(new[] { "'self'", "cdn.example.com" }));
			Assert.That(stored.Sources.Select(s => s.DefinitionId), Is.All.EqualTo(stored.Id));
		});

		var frontend = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		Assert.That(frontend.Sources, Has.Count.EqualTo(2), "the frontend policy must be left untouched");
	}

	// Creating a node policy means it should apply, so it starts on even when Frontend is off.
	[Test]
	public async Task CreateCspDefinitionForDomainAsync_WhenTheFrontendPolicyIsDisabled_CreatesItEnabled()
	{
		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = false,
			Sources = [new CspDefinitionSource { DefinitionId = Constants.DefaultFrontEndId, Source = "'self'", Directives = [Constants.Directives.DefaultSource] }]
		}, CancellationToken.None);

		var created = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionAsync(created.Id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(stored?.Enabled, Is.True);
			Assert.That(stored?.Sources.Select(s => s.Source), Is.EqualTo(new[] { "'self'" }), "everything else is still copied");
		});
		Assert.That((await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None)).Enabled, Is.False,
			"the frontend policy must be left untouched");
	}

	[Test]
	public async Task SaveCspDefinitionAsync_WithEmptyIdAndContentKey_CreatesWithServerAssignedId()
	{
		var saved = await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "'self'"), CancellationToken.None);

		Assert.That(saved.Id, Is.Not.EqualTo(Guid.Empty));
		var stored = await _cspService.GetCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		Assert.That(stored?.Id, Is.EqualTo(saved.Id));
		Assert.That(stored.Sources.Single().DefinitionId, Is.EqualTo(saved.Id));
	}

	[Test]
	public void SaveCspDefinitionAsync_SecondPolicyForTheSameDomain_IsRejected()
	{
		Assert.DoesNotThrowAsync(() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA), CancellationToken.None));

		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, Guid.NewGuid()), CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
	}

	[Test]
	public void SaveCspDefinitionAsync_ForAnUnknownNode_IsRejected()
	{
		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(Guid.NewGuid()), CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
	}

	[Test]
	public void SaveCspDefinitionAsync_ForANodeWithOnlyACultureDomain_IsRejected()
	{
		Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_wildcardOnlyNode), CancellationToken.None));
	}

	// ── Identity / hijack ────────────────────────────────────────────────────

	[Test]
	public async Task SaveCspDefinitionAsync_GlobalIdWithContentKey_IsRejectedAndLeavesTheGlobalPolicyAlone()
	{
		var id = Constants.DefaultFrontEndId;
		await SaveFrontendAsync("'self'");

		Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, id, "evil.example.com"), CancellationToken.None));

		var stored = await _cspService.GetCspDefinitionAsync(id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(stored.ContentKey, Is.Null);
			Assert.That(stored.Sources.Select(s => s.Source), Does.Not.Contain("evil.example.com"));
		});
		Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_nodeA, CancellationToken.None), Is.Null);
	}

	[Test]
	public async Task SaveCspDefinitionAsync_ExistingDomainPolicyWithAnotherContentKey_IsRejected()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeB, policy.Id), CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));

		var stored = await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None);
		Assert.That(stored.ContentKey, Is.EqualTo(_nodeA));
	}

	[Test]
	public void SaveCspDefinitionAsync_NonGlobalIdWithoutContentKey_IsRejected()
	{
		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(() => _cspService.SaveCspDefinitionAsync(
			new CspDefinition { Id = Guid.NewGuid(), Enabled = true }, CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.Id)));
	}

	[Test]
	public async Task SaveCspDefinitionAsync_DomainPolicyFlaggedAsBackOffice_IsSavedAsFrontend()
	{
		var definition = NewDomainPolicy(_nodeA);
		definition.IsBackOffice = true;

		var saved = await _cspService.SaveCspDefinitionAsync(definition, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionAsync(saved.Id, CancellationToken.None);
		Assert.That(stored.IsBackOffice, Is.False);
		var backoffice = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);
		Assert.That(backoffice.Id, Is.EqualTo(Constants.DefaultBackofficeId));
	}

	[Test]
	public async Task SaveCspDefinitionAsync_GlobalId_TakesIsBackOfficeFromTheId()
	{
		await _cspService.SaveCspDefinitionAsync(new CspDefinition { Id = Constants.DefaultFrontEndId, IsBackOffice = true }, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionAsync(Constants.DefaultFrontEndId, CancellationToken.None);
		Assert.That(stored.IsBackOffice, Is.False);
		var backoffice = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: true, CancellationToken.None);
		Assert.That(backoffice.Id, Is.EqualTo(Constants.DefaultBackofficeId));
	}

	[Test]
	public async Task SaveCspDefinitionAsync_SourcesPointingAtAnotherDefinition_AreSavedOnTheSavedDefinition()
	{
		await SaveFrontendAsync("'self'");
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		policy.Sources.Add(new CspDefinitionSource
		{
			DefinitionId = Constants.DefaultFrontEndId,
			Source = "injected.example.com",
			Directives = [Constants.Directives.ScriptSource]
		});
		await _cspService.SaveCspDefinitionAsync(policy, CancellationToken.None);

		var frontend = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);
		var stored = await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(frontend.Sources.Select(s => s.Source), Does.Not.Contain("injected.example.com"));
			Assert.That(stored.Sources.Select(s => s.Source), Does.Contain("injected.example.com"));
		});
	}

	[Test]
	public async Task SaveCspDefinitionAsync_ExistingDomainPolicy_UpdatesIt()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		policy.Enabled = false;
		policy.Sources = [new CspDefinitionSource { Source = "only.example.com", Directives = [Constants.Directives.ImageSource] }];

		await _cspService.SaveCspDefinitionAsync(policy, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(stored.Enabled, Is.False);
			Assert.That(stored.Sources.Select(s => s.Source), Is.EqualTo(new[] { "only.example.com" }));
		});
	}

	// ── Queries ──────────────────────────────────────────────────────────────

	[Test]
	public async Task GetCspDefinitionAsync_Global_ExcludesDomainPolicies()
	{
		// No frontend row is persisted, only a node policy (also IsBackOffice = false).
		await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "domain-only.example.com"), CancellationToken.None);

		var frontend = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(frontend.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(frontend.ContentKey, Is.Null);
			Assert.That(frontend.Sources, Is.Empty);
		});
	}

	[Test]
	public async Task GetAllDomainPoliciesAsync_ReturnsOnlyDomainPoliciesWithTheirSources()
	{
		await SaveFrontendAsync("'self'");
		var a = await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: ["a1", "a2"]), CancellationToken.None);
		var b = await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeB, sources: ["b1"]), CancellationToken.None);

		var policies = await _cspService.GetAllNodePoliciesAsync(CancellationToken.None);

		Assert.That(policies.Select(p => p.Id), Is.EquivalentTo(new[] { a.Id, b.Id }));
		Assert.Multiple(() =>
		{
			Assert.That(policies.Single(p => p.Id == a.Id).Sources.Select(s => s.Source), Is.EquivalentTo(new[] { "a1", "a2" }));
			Assert.That(policies.Single(p => p.Id == b.Id).Sources.Select(s => s.Source), Is.EquivalentTo(new[] { "b1" }));
		});
	}

	// ── Delete ───────────────────────────────────────────────────────────────

	[Test]
	public async Task DeleteCspDefinitionAsync_RemovesThePolicyAndItsSources_AndPublishesDeleted()
	{
		CspDeletedNotification published = null;
		var eventAggregator = Mock.Of<IEventAggregator>();
		Mock.Get(eventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspDeletedNotification>(), It.IsAny<CancellationToken>()))
			.Callback<CspDeletedNotification, CancellationToken>((n, _) => published = n)
			.Returns(Task.CompletedTask);
		var service = CreateService(AppCaches.Create(NoAppCache.Instance), eventAggregator);

		var policy = await service.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "x.example.com"), CancellationToken.None);

		await service.DeleteCspDefinitionAsync(policy.Id, CancellationToken.None);

		Assert.That(await service.GetCspDefinitionAsync(policy.Id, CancellationToken.None), Is.Null);
		using (var scope = ScopeProvider.CreateScope(autoComplete: true))
		{
			var orphanSources = scope.Database.ExecuteScalar<int>(
				scope.SqlContext.Sql().SelectCount().From<CspDefinitionSource>().Where<CspDefinitionSource>(x => x.DefinitionId == policy.Id));
			Assert.That(orphanSources, Is.Zero);
		}

		Assert.That(published, Is.Not.Null);
		Assert.Multiple(() =>
		{
			Assert.That(published.CspDefinition.Id, Is.EqualTo(policy.Id));
			Assert.That(published.CspDefinition.ContentKey, Is.EqualTo(_nodeA));
		});
		Mock.Get(eventAggregator).Verify(x => x.PublishAsync(It.IsAny<CspSavedNotification>(), It.IsAny<CancellationToken>()), Times.Once,
			"only the create should have published a saved notification; the delete must not");
	}

	[Test]
	public void DeleteCspDefinitionAsync_UnknownId_DoesNothing()
	{
		var eventAggregator = Mock.Of<IEventAggregator>();
		var service = CreateService(AppCaches.Create(NoAppCache.Instance), eventAggregator);

		Assert.DoesNotThrowAsync(() => service.DeleteCspDefinitionAsync(Guid.NewGuid(), CancellationToken.None));
		Mock.Get(eventAggregator).Verify(x => x.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Test]
	public async Task OrphanedPolicy_IsKeptWhenItsNodeLosesItsHostnames()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		await CspTestDomainHelper.AssignDomainsAsync(_domainService, Textpage.Key, []);

		var policies = await _cspService.GetAllNodePoliciesAsync(CancellationToken.None);
		Assert.That(policies.Select(p => p.Id), Does.Contain(policy.Id));
		Assert.DoesNotThrowAsync(() => _cspService.DeleteCspDefinitionAsync(policy.Id, CancellationToken.None));
	}

	// Umbraco keeps a trashed node's domains, but never routes a request to it.
	[Test]
	public async Task SaveCspDefinitionAsync_ForANodeInTheRecycleBin_IsRejected()
	{
		GetRequiredService<IContentService>().MoveToRecycleBin(Textpage);

		Assert.That(await _cspService.ContentNodeHasHostnameAsync(_nodeA, CancellationToken.None), Is.False);
		Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA), CancellationToken.None));
	}

	// ── Content node semantics ───────────────────────────────────────────────

	// A multilingual site: one node, one hostname per culture, one policy for all of them.
	[Test]
	public async Task ANodeWithSeveralHostnames_HasExactlyOnePolicy()
	{
		await CspTestDomainHelper.AssignDomainsAsync(_domainService, Textpage.Key, ["a.example.com/nl", "a.example.com/fr", "a.example.de"]);

		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		Assert.That(policy.ContentKey, Is.EqualTo(_nodeA));
		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.CreateCspDefinitionForNodeAsync(_nodeA, CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
	}

	// Renaming a hostname used to orphan a policy keyed on the hostname; keyed on the node it stays in use.
	[Test]
	public async Task RenamingAHostname_KeepsThePolicyInUseAndEditable()
	{
		var policy = await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "'self'"), CancellationToken.None);

		await CspTestDomainHelper.AssignDomainsAsync(_domainService, Textpage.Key, ["www.renamed.example.com"]);

		var stored = await _cspService.GetCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		Assert.That(stored?.Id, Is.EqualTo(policy.Id));
		stored!.Sources = [new CspDefinitionSource { Source = "cdn.example.com", Directives = [Constants.Directives.DefaultSource] }];
		Assert.DoesNotThrowAsync(() => _cspService.SaveCspDefinitionAsync(stored, CancellationToken.None));
	}

	[Test]
	public void SaveCspDefinitionAsync_ForANodeWithoutAnyDomain_IsRejected()
	{
		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(
			() => _cspService.SaveCspDefinitionAsync(NewDomainPolicy(Subpage3.Key), CancellationToken.None));
		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
	}

	// ── Cache ────────────────────────────────────────────────────────────────

	[Test]
	public async Task GetCachedCspDefinitionForDomainAsync_ReturnsIndependentCopiesThatKeepTheContentKey()
	{
		var service = CreateService(AppCaches.Create(NoAppCache.Instance));
		await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "'self'"), CancellationToken.None);

		var first = await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		first.Sources.Add(new CspDefinitionSource { Source = "mutated-by-caller", Directives = [Constants.Directives.ConnectSource] });
		var second = await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(second, Is.Not.SameAs(first));
			Assert.That(second.ContentKey, Is.EqualTo(_nodeA));
			Assert.That(second.Sources.Select(s => s.Source), Does.Not.Contain("mutated-by-caller"));
		});
	}

	[Test]
	public async Task GetCachedCspDefinitionForDomainAsync_CachesTheAbsenceOfAPolicy()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = CreateService(caches);

		Assert.That(await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None), Is.Null);
		Assert.That(caches.RuntimeCache.Get(Constants.NodeCacheKey(_nodeA)), Is.Not.Null,
			"a domain without a policy should be cached too, or every request on it hits the database");

		// Created through another cache instance, so `service`'s negative entry isn't invalidated.
		await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA), CancellationToken.None);

		Assert.That(await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None), Is.Null,
			"the second lookup should be served from the cache, not the database");
	}

	[Test]
	public async Task GetCachedCspDefinitionForDomainAsync_CachesDomainsSeparately()
	{
		await _cspService.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "a.only"), CancellationToken.None);
		var service = CreateService(AppCaches.Create(NoAppCache.Instance));

		var a = await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		var b = await service.GetCachedCspDefinitionForNodeAsync(_nodeB, CancellationToken.None);
		var frontend = await service.GetCachedCspDefinitionAsync(isBackOfficeRequest: false, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(a?.Sources.Single().Source, Is.EqualTo("a.only"));
			Assert.That(b, Is.Null);
			Assert.That(frontend.ContentKey, Is.Null);
			Assert.That(frontend.Sources, Is.Empty);
		});
	}

	// Service + saved/deleted handler against one shared real cache: creating a policy must clear
	// the domain's negative entry, saving must clear the cached policy, deleting must clear it too.
	[Test]
	public async Task SaveAndDelete_InvalidateTheCachedDomainPolicy()
	{
		var caches = AppCaches.Create(NoAppCache.Instance);
		var distributedCache = new DistributedCache(
			new SpyServerMessenger(),
			new CacheRefresherCollection(() => new ICacheRefresher[] { new StubCacheRefresher() }));
		var handler = new CspSavedNotificationHandler(caches, distributedCache);

		var eventAggregator = Mock.Of<IEventAggregator>();
		Mock.Get(eventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspSavedNotification>(), It.IsAny<CancellationToken>()))
			.Callback<CspSavedNotification, CancellationToken>((n, _) => handler.Handle(n))
			.Returns(Task.CompletedTask);
		Mock.Get(eventAggregator)
			.Setup(x => x.PublishAsync(It.IsAny<CspDeletedNotification>(), It.IsAny<CancellationToken>()))
			.Callback<CspDeletedNotification, CancellationToken>((n, _) => handler.Handle(n))
			.Returns(Task.CompletedTask);
		var service = CreateService(caches, eventAggregator);

		Assert.That(await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None), Is.Null);

		var created = await service.SaveCspDefinitionAsync(NewDomainPolicy(_nodeA, sources: "v1"), CancellationToken.None);
		var afterCreate = await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		Assert.That(afterCreate?.Sources.Single().Source, Is.EqualTo("v1"));

		created.Sources = [new CspDefinitionSource { Source = "v2", Directives = [Constants.Directives.DefaultSource] }];
		await service.SaveCspDefinitionAsync(created, CancellationToken.None);
		var afterSave = await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None);
		Assert.That(afterSave?.Sources.Single().Source, Is.EqualTo("v2"));

		await service.DeleteCspDefinitionAsync(created.Id, CancellationToken.None);
		Assert.That(await service.GetCachedCspDefinitionForNodeAsync(_nodeA, CancellationToken.None), Is.Null);
	}
}
