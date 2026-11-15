using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Notifications.Handlers;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Tests.Helpers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Services;

/// <summary>
/// Node policies on any content node, per culture, with inheritance: what can be saved, and which
/// policy a request gets, against a real content tree, real languages and a real database.
/// </summary>
/// <remarks>
/// The content tree is Textpage (root) with Subpage, Subpage2 and Subpage3 below it. The languages
/// are en-US (default), fr-FR, fr-CA (falls back to fr-FR) and da-DK.
/// </remarks>
[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class CspServiceNodePolicyResolutionTests : UmbracoIntegrationTestWithContent
{
	private ICspService _cspService;
	private ILanguageService _languageService;
	private Guid _home;
	private Guid _page;
	private Guid _otherPage;

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
	public async Task SetUpSite()
	{
		await CspTestMigrationHelper.RunMigrationsAsync(GetRequiredService<IMigrationPlanExecutor>(), ScopeProvider, GetRequiredService<IKeyValueService>());
		_cspService = GetRequiredService<ICspService>();
		_languageService = GetRequiredService<ILanguageService>();

		await CreateLanguageAsync("fr-FR");
		await CreateLanguageAsync("fr-CA", fallback: "fr-FR");
		await CreateLanguageAsync("da-DK");

		_home = Textpage.Key;
		_page = Subpage.Key;
		_otherPage = Subpage2.Key;

		await RebuildNavigationAsync();
	}

	// A running site builds Umbraco's in-memory navigation structure at startup and keeps it up to
	// date; the integration test host doesn't, so the tests build it from the database.
	private Task RebuildNavigationAsync() => GetRequiredService<IDocumentNavigationManagementService>().RebuildAsync();

	private Task CreateLanguageAsync(string isoCode, string fallback = null)
		=> CspTestLanguageHelper.CreateLanguageAsync(_languageService, isoCode, fallback);

	private CspService CreateService(AppCaches caches, DisabledNodePolicyBehavior disabledBehavior = DisabledNodePolicyBehavior.FallbackToGlobal)
		=> CspTestServiceFactory.Create(
			Services,
			caches,
			options: Mock.Of<IOptionsMonitor<CspManagerOptions>>(o => o.CurrentValue == new CspManagerOptions { DisabledNodePolicyBehavior = disabledBehavior }));

	private static CspDefinition NewPolicy(
		Guid contentKey,
		string source,
		string culture = null,
		CspPolicyInheritance inheritance = CspPolicyInheritance.Replace,
		CspPolicyScope appliesTo = CspPolicyScope.Descendants,
		bool enabled = true,
		string directive = Constants.Directives.DefaultSource)
		=> new()
		{
			Id = Guid.Empty,
			ContentKey = contentKey,
			Culture = culture,
			Inheritance = inheritance,
			AppliesTo = appliesTo,
			Enabled = enabled,
			Sources = [new CspDefinitionSource { Source = source, Directives = [directive] }]
		};

	private Task<CspDefinition> SaveAsync(CspDefinition definition)
		=> _cspService.SaveCspDefinitionAsync(definition, CancellationToken.None);

	private Task<CspDefinition> SaveFrontendAsync(string source, bool enabled = true, bool reportOnly = false)
		=> SaveAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = enabled,
			ReportOnly = reportOnly,
			Sources = [new CspDefinitionSource { Source = source, Directives = [Constants.Directives.DefaultSource] }]
		});

	private Task<CspDefinition> ResolveAsync(Guid? page, string culture = null, Guid? domainNode = null)
		=> _cspService.ResolveCspDefinitionAsync(page, domainNode, culture, CancellationToken.None);

	private static string[] SourcesOf(CspDefinition definition) => [.. definition.Sources.Select(s => s.Source)];

	// ── Any content node ─────────────────────────────────────────────────────

	[Test]
	public async Task Resolve_WithoutNodePolicies_ReturnsNull()
	{
		await SaveFrontendAsync("frontend.example.com");

		Assert.That(await ResolveAsync(_page, "en-US"), Is.Null);
	}

	[Test]
	public async Task APageWithoutAHostname_CanHaveItsOwnPolicy()
	{
		var policy = await SaveAsync(NewPolicy(_page, "page.example.com"));

		var resolved = await ResolveAsync(_page);

		Assert.Multiple(() =>
		{
			Assert.That(resolved?.Id, Is.EqualTo(policy.Id));
			Assert.That(SourcesOf(resolved), Is.EqualTo(new[] { "page.example.com" }));
		});
	}

	[Test]
	public async Task Resolve_PageBelowANodeWithAPolicy_GetsThatPolicy()
	{
		var home = await SaveAsync(NewPolicy(_home, "home.example.com"));

		var resolved = await ResolveAsync(_page);

		Assert.Multiple(() =>
		{
			Assert.That(resolved?.Id, Is.EqualTo(home.Id));
			Assert.That(resolved?.ContentKey, Is.EqualTo(_home));
		});
	}

	[Test]
	public async Task Resolve_PolicyForOnePageOnly_DoesNotApplyToThePagesBelowIt()
	{
		await SaveAsync(NewPolicy(_home, "home-only.example.com", appliesTo: CspPolicyScope.NodeOnly));

		Assert.Multiple(async () =>
		{
			Assert.That(SourcesOf(await ResolveAsync(_home)), Is.EqualTo(new[] { "home-only.example.com" }));
			Assert.That(await ResolveAsync(_page), Is.Null);
		});
	}

	[Test]
	public async Task Resolve_PolicyOnASibling_DoesNotApply()
	{
		await SaveAsync(NewPolicy(_otherPage, "other.example.com"));

		Assert.That(await ResolveAsync(_page), Is.Null);
	}

	// The ancestors come from Umbraco's live navigation structure, so nothing of ours is stale
	// after a move.
	[Test]
	public async Task Resolve_AfterAPageIsMoved_FollowsItsNewAncestors()
	{
		var contentService = GetRequiredService<IContentService>();
		var otherSite = ContentBuilder.CreateSimpleContent(ContentType, "Other site");
		contentService.Save(otherSite);
		await RebuildNavigationAsync();
		await SaveAsync(NewPolicy(_home, "home.example.com"));
		await SaveAsync(NewPolicy(otherSite.Key, "other-site.example.com"));
		Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "home.example.com" }));

		contentService.Move(Subpage, otherSite.Id);
		await RebuildNavigationAsync();

		Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "other-site.example.com" }));
	}

	// Content served through a hostname from outside that hostname's site, such as a shared
	// "not found" page: the site's policy still applies.
	[Test]
	public async Task Resolve_PageOutsideTheSiteOfTheHostname_GetsThePolicyOfTheHostnamesNode()
	{
		var contentService = GetRequiredService<IContentService>();
		var notFound = ContentBuilder.CreateSimpleContent(ContentType, "Not found");
		contentService.Save(notFound);
		await RebuildNavigationAsync();
		await SaveAsync(NewPolicy(_home, "home.example.com"));

		Assert.Multiple(async () =>
		{
			Assert.That(await ResolveAsync(notFound.Key), Is.Null);
			Assert.That(SourcesOf(await ResolveAsync(notFound.Key, domainNode: _home)), Is.EqualTo(new[] { "home.example.com" }));
			Assert.That(SourcesOf(await ResolveAsync(page: null, domainNode: _home)), Is.EqualTo(new[] { "home.example.com" }));
		});
	}

	// The same "not found" page stored under one site and served on the hostname of another: the
	// visitor is on the other site, so that site decides, as it does for its own pages.
	[Test]
	public async Task Resolve_PageOfAnotherSiteServedOnAHostname_GetsThePolicyOfTheHostnamesSite()
	{
		var contentService = GetRequiredService<IContentService>();
		var siteB = ContentBuilder.CreateSimpleContent(ContentType, "Site B");
		contentService.Save(siteB);
		var pageB = ContentBuilder.CreateSimpleContent(ContentType, "Page B", siteB.Id);
		contentService.Save(pageB);
		await RebuildNavigationAsync();
		await SaveAsync(NewPolicy(_home, "home.example.com"));

		// Site B has no policy: its hostname gets the frontend policy, also for a page of Home.
		Assert.That(await ResolveAsync(_page, domainNode: siteB.Key), Is.Null);

		await SaveAsync(NewPolicy(siteB.Key, "site-b.example.com"));
		Assert.Multiple(async () =>
		{
			Assert.That(SourcesOf(await ResolveAsync(_page, domainNode: siteB.Key)), Is.EqualTo(new[] { "site-b.example.com" }));
			Assert.That(SourcesOf(await ResolveAsync(_page, domainNode: pageB.Key)), Is.EqualTo(new[] { "site-b.example.com" }),
				"the nodes above the hostname's node count, as they do for its own pages");
			Assert.That(SourcesOf(await ResolveAsync(_page, domainNode: _home)), Is.EqualTo(new[] { "home.example.com" }));
		});

		// A policy on the page itself is still the most specific one, on any hostname.
		await SaveAsync(NewPolicy(_page, "page.example.com", inheritance: CspPolicyInheritance.Extend, appliesTo: CspPolicyScope.NodeOnly));
		Assert.That(SourcesOf(await ResolveAsync(_page, domainNode: siteB.Key)), Is.EqualTo(new[] { "site-b.example.com", "page.example.com" }));
	}

	[Test]
	public async Task Resolve_ReturnsACopyThatIsSafeToChange()
	{
		await SaveAsync(NewPolicy(_home, "home.example.com"));
		var service = CreateService(AppCaches.Create(NoAppCache.Instance));

		var first = await service.ResolveCspDefinitionAsync(_page, null, null, CancellationToken.None);
		first.Sources.Add(new CspDefinitionSource { Source = "mutated-by-caller", Directives = [Constants.Directives.ConnectSource] });
		first.Sources[0].Directives.Add("mutated");
		var second = await service.ResolveCspDefinitionAsync(_page, null, null, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(SourcesOf(second), Is.EqualTo(new[] { "home.example.com" }));
			Assert.That(second.Sources[0].Directives, Is.EqualTo(new[] { Constants.Directives.DefaultSource }));
		});
	}

	// ── Cultures ─────────────────────────────────────────────────────────────

	[Test]
	public async Task ANode_CanHaveAPolicyPerCultureNextToItsPolicyForAllCultures()
	{
		var all = await SaveAsync(NewPolicy(_home, "all.example.com"));
		var french = await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));
		var danish = await SaveAsync(NewPolicy(_home, "dk.example.com", culture: "da-DK"));

		Assert.Multiple(async () =>
		{
			Assert.That((await ResolveAsync(_home, "fr-FR"))?.Id, Is.EqualTo(french.Id));
			Assert.That((await ResolveAsync(_home, "da-DK"))?.Id, Is.EqualTo(danish.Id));
			Assert.That((await ResolveAsync(_home, "en-US"))?.Id, Is.EqualTo(all.Id));
			Assert.That((await ResolveAsync(_home))?.Id, Is.EqualTo(all.Id));
			Assert.That((await _cspService.GetAllNodePoliciesAsync(CancellationToken.None)).Select(p => p.Id),
				Is.EquivalentTo(new[] { all.Id, french.Id, danish.Id }));
		});
	}

	[Test]
	public async Task Resolve_CultureWithoutItsOwnPolicy_FollowsTheLanguageFallback()
	{
		var french = await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));
		await SaveAsync(NewPolicy(_home, "all.example.com"));

		var resolved = await ResolveAsync(_page, "fr-CA");

		Assert.Multiple(() =>
		{
			Assert.That(resolved?.Id, Is.EqualTo(french.Id));
			Assert.That(resolved?.Culture, Is.EqualTo("fr-FR"));
		});
	}

	[Test]
	public async Task Resolve_CultureWithoutAPolicyInItsFallbackChain_UsesThePolicyForAllCultures()
	{
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));
		var all = await SaveAsync(NewPolicy(_home, "all.example.com"));

		Assert.That((await ResolveAsync(_page, "da-DK"))?.Id, Is.EqualTo(all.Id));
	}

	[Test]
	public async Task Resolve_OnlyAPolicyForAnotherCulture_ReturnsNull()
	{
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));

		Assert.That(await ResolveAsync(_page, "da-DK"), Is.Null);
	}

	// Languages rarely change, so the fallback chains are cached; Umbraco's own language cache
	// refresh is what clears them.
	[Test]
	public async Task Resolve_AfterALanguagesFallbackChanges_FollowsTheNewFallbackOnceTheLanguageCacheIsRefreshed()
	{
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));
		var caches = AppCaches.Create(NoAppCache.Instance);
		var service = CreateService(caches);
		Assert.That(await service.ResolveCspDefinitionAsync(_page, null, "fr-CA", CancellationToken.None), Is.Not.Null);

		var canadian = await _languageService.GetAsync("fr-CA");
		canadian!.FallbackIsoCode = null;
		var updated = await _languageService.UpdateAsync(canadian, UmbConstants.Security.SuperUserKey);
		Assert.That(updated.Success, Is.True, updated.Status.ToString());

		Assert.That(await service.ResolveCspDefinitionAsync(_page, null, "fr-CA", CancellationToken.None), Is.Not.Null,
			"the chains are served from the cache until Umbraco refreshes its languages");

		new CspLanguageCacheHandler(caches).Handle(new LanguageCacheRefresherNotification(Array.Empty<object>(), MessageType.RefreshByPayload));

		Assert.That(await service.ResolveCspDefinitionAsync(_page, null, "fr-CA", CancellationToken.None), Is.Null);
	}

	// ── Inherit and add ──────────────────────────────────────────────────────

	// The Contact page allows Google Maps and otherwise keeps following the site's policy.
	[Test]
	public async Task Resolve_ExtendingPolicyOnAPage_AddsItsSourcesToTheGlobalFrontendPolicy()
	{
		await SaveFrontendAsync("'self'", reportOnly: true);
		var contact = await SaveAsync(NewPolicy(_page, "https://maps.googleapis.com", inheritance: CspPolicyInheritance.Extend, appliesTo: CspPolicyScope.NodeOnly, directive: Constants.Directives.ScriptSource));

		var resolved = await ResolveAsync(_page);

		Assert.Multiple(async () =>
		{
			Assert.That(resolved?.Id, Is.EqualTo(contact.Id));
			Assert.That(resolved?.ContentKey, Is.EqualTo(_page));
			Assert.That(resolved?.ReportOnly, Is.True, "the settings are those of the policy it extends");
			Assert.That(SourcesOf(resolved), Is.EqualTo(new[] { "'self'", "https://maps.googleapis.com" }));
			Assert.That(await ResolveAsync(_otherPage), Is.Null, "other pages keep the frontend policy as it is");
		});
	}

	[Test]
	public async Task Resolve_ExtendingPolicyOnAPage_AddsItsSourcesToThePolicyOfAnAncestor()
	{
		await SaveFrontendAsync("frontend.example.com");
		await SaveAsync(NewPolicy(_home, "home.example.com"));
		await SaveAsync(NewPolicy(_page, "https://maps.googleapis.com", inheritance: CspPolicyInheritance.Extend));

		Assert.Multiple(async () =>
		{
			Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "home.example.com", "https://maps.googleapis.com" }));
			Assert.That(SourcesOf(await ResolveAsync(_otherPage)), Is.EqualTo(new[] { "home.example.com" }));
		});
	}

	// The extended policy is read when the request is resolved, so a change to it shows up on the
	// pages that extend it without saving them again.
	[Test]
	public async Task Resolve_ExtendingPolicy_FollowsLaterChangesToThePolicyItExtends()
	{
		var home = await SaveAsync(NewPolicy(_home, "v1.example.com"));
		await SaveAsync(NewPolicy(_page, "https://maps.googleapis.com", inheritance: CspPolicyInheritance.Extend));
		Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "v1.example.com", "https://maps.googleapis.com" }));

		home.Sources = [new CspDefinitionSource { Source = "v2.example.com", Directives = [Constants.Directives.DefaultSource] }];
		await SaveAsync(home);

		Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "v2.example.com", "https://maps.googleapis.com" }));
	}

	[Test]
	public async Task Resolve_ExtendingADisabledFrontendPolicy_StaysDisabled()
	{
		await SaveFrontendAsync("'self'", enabled: false);
		await SaveAsync(NewPolicy(_page, "https://maps.googleapis.com", inheritance: CspPolicyInheritance.Extend));

		var resolved = await ResolveAsync(_page);

		Assert.That(resolved?.Enabled, Is.False, "adding sources to a policy that is off must not switch a header on");
	}

	// ── Disabled policies ────────────────────────────────────────────────────

	[Test]
	public async Task Resolve_DisabledPolicy_FallsBackToTheNextPolicyUpTheTree()
	{
		var home = await SaveAsync(NewPolicy(_home, "home.example.com"));
		await SaveAsync(NewPolicy(_page, "page.example.com", enabled: false));

		Assert.Multiple(async () =>
		{
			Assert.That((await ResolveAsync(_page))?.Id, Is.EqualTo(home.Id));
			Assert.That(await ResolveAsync(_page) is { Enabled: true }, Is.True);
		});
	}

	[Test]
	public async Task Resolve_DisabledPolicy_WithNoHeaderBehaviour_ReturnsTheDisabledPolicy()
	{
		await SaveAsync(NewPolicy(_home, "home.example.com"));
		var page = await SaveAsync(NewPolicy(_page, "page.example.com", enabled: false));
		var service = CreateService(AppCaches.Create(NoAppCache.Instance), DisabledNodePolicyBehavior.NoHeader);

		var resolved = await service.ResolveCspDefinitionAsync(_page, null, null, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(resolved?.Id, Is.EqualTo(page.Id));
			Assert.That(resolved?.Enabled, Is.False);
		});
	}

	// ── Creating ─────────────────────────────────────────────────────────────

	[Test]
	public async Task Create_CompletePolicyBelowANodeWithAPolicy_StartsAsACopyOfThatPolicy()
	{
		await SaveFrontendAsync("frontend.example.com");
		var home = NewPolicy(_home, "home.example.com");
		home.ReportOnly = true;
		await SaveAsync(home);

		var created = await _cspService.CreateCspDefinitionForNodeAsync(_page, null, CspPolicyInheritance.Replace, CspPolicyScope.NodeOnly, CancellationToken.None);

		var stored = await _cspService.GetCspDefinitionAsync(created.Id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(stored.ContentKey, Is.EqualTo(_page));
			Assert.That(stored.Culture, Is.Null);
			Assert.That(stored.Inheritance, Is.EqualTo(CspPolicyInheritance.Replace));
			Assert.That(stored.AppliesTo, Is.EqualTo(CspPolicyScope.NodeOnly));
			Assert.That(stored.Enabled, Is.True);
			Assert.That(stored.ReportOnly, Is.True);
			Assert.That(SourcesOf(stored), Is.EqualTo(new[] { "home.example.com" }));
			Assert.That(stored.Sources.Select(s => s.DefinitionId), Is.All.EqualTo(stored.Id));
		});
	}

	[Test]
	public async Task Create_CompletePolicyForACulture_StartsAsACopyOfWhatThatCultureGetsToday()
	{
		await SaveAsync(NewPolicy(_home, "all.example.com"));
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));

		var created = await _cspService.CreateCspDefinitionForNodeAsync(_home, "fr-CA", CspPolicyInheritance.Replace, CspPolicyScope.Descendants, CancellationToken.None);

		Assert.Multiple(() =>
		{
			Assert.That(created.Culture, Is.EqualTo("fr-CA"));
			Assert.That(SourcesOf(created), Is.EqualTo(new[] { "fr.example.com" }));
		});
	}

	[Test]
	public async Task Create_ExtendingPolicy_StartsEnabledWithoutSources()
	{
		await SaveFrontendAsync("frontend.example.com");

		var created = await _cspService.CreateCspDefinitionForNodeAsync(_page, null, CspPolicyInheritance.Extend, CspPolicyScope.Descendants, CancellationToken.None);

		Assert.Multiple(async () =>
		{
			Assert.That(created.Inheritance, Is.EqualTo(CspPolicyInheritance.Extend));
			Assert.That(created.Enabled, Is.True);
			Assert.That(created.Sources, Is.Empty);
			Assert.That(SourcesOf(await ResolveAsync(_page)), Is.EqualTo(new[] { "frontend.example.com" }), "creating it changes no header");
		});
	}

	// ── Validation ───────────────────────────────────────────────────────────

	[Test]
	public async Task Save_NewPolicyForACulture_StoresTheLanguagesOwnSpellingOfTheCulture()
	{
		var saved = await SaveAsync(NewPolicy(_home, "fr.example.com", culture: " FR-fr "));

		var stored = await _cspService.GetCspDefinitionAsync(saved.Id, CancellationToken.None);
		Assert.That(stored.Culture, Is.EqualTo("fr-FR"));
	}

	[Test]
	public void Save_NewPolicyForACultureThatIsNotALanguage_IsRejected()
	{
		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(NewPolicy(_home, "x.example.com", culture: "nl-NL")));

		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.Culture)));
	}

	[Test]
	public async Task Save_SecondPolicyForTheSameNodeAndCulture_IsRejected()
	{
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));

		var ex = Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(NewPolicy(_home, "again.example.com", culture: "FR-FR")));

		Assert.That(ex!.MemberName, Is.EqualTo(nameof(CspDefinition.ContentKey)));
	}

	[Test]
	public async Task Save_ExistingPolicyWithAnotherCulture_IsRejected()
	{
		var french = await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));

		var moved = NewPolicy(_home, "fr.example.com", culture: "da-DK");
		moved.Id = french.Id;
		var toAllCultures = NewPolicy(_home, "fr.example.com");
		toAllCultures.Id = french.Id;

		Assert.Multiple(async () =>
		{
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(moved))!.MemberName, Is.EqualTo(nameof(CspDefinition.Culture)));
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(toAllCultures))!.MemberName, Is.EqualTo(nameof(CspDefinition.Culture)));
			Assert.That((await _cspService.GetCspDefinitionAsync(french.Id, CancellationToken.None)).Culture, Is.EqualTo("fr-FR"));
		});
	}

	[Test]
	public async Task Save_ExistingPolicy_CanChangeHowItInheritsAndWhatItAppliesTo()
	{
		var policy = await SaveAsync(NewPolicy(_page, "page.example.com"));

		policy.Inheritance = CspPolicyInheritance.Extend;
		policy.AppliesTo = CspPolicyScope.NodeOnly;
		await SaveAsync(policy);

		var stored = await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(stored.Inheritance, Is.EqualTo(CspPolicyInheritance.Extend));
			Assert.That(stored.AppliesTo, Is.EqualTo(CspPolicyScope.NodeOnly));
		});
	}

	// A deleted language leaves its policies unused, but they must stay editable and deletable.
	[Test]
	public async Task Save_And_Delete_ExistingPolicyWhoseLanguageWasDeleted_StillWork()
	{
		var danish = await SaveAsync(NewPolicy(_home, "dk.example.com", culture: "da-DK"));
		var deleted = await _languageService.DeleteAsync("da-DK", UmbConstants.Security.SuperUserKey);
		Assert.That(deleted.Success, Is.True, deleted.Status.ToString());

		danish.Enabled = false;

		Assert.Multiple(() =>
		{
			Assert.DoesNotThrowAsync(() => SaveAsync(danish));
			Assert.DoesNotThrowAsync(() => _cspService.DeleteCspDefinitionAsync(danish.Id, CancellationToken.None));
		});
	}

	[Test]
	public void Save_GlobalPolicyWithACultureOrInheritance_IsRejected()
	{
		var withCulture = new CspDefinition { Id = Constants.DefaultFrontEndId, Culture = "fr-FR" };
		var extending = new CspDefinition { Id = Constants.DefaultFrontEndId, Inheritance = CspPolicyInheritance.Extend };

		Assert.Multiple(() =>
		{
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(withCulture))!.MemberName, Is.EqualTo(nameof(CspDefinition.Culture)));
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(extending))!.MemberName, Is.EqualTo(nameof(CspDefinition.Inheritance)));
		});
	}

	[Test]
	public void Save_UnknownInheritanceOrAppliesTo_IsRejected()
	{
		var inheritance = NewPolicy(_home, "x.example.com", inheritance: (CspPolicyInheritance)7);
		var appliesTo = NewPolicy(_home, "x.example.com", appliesTo: (CspPolicyScope)7);

		Assert.Multiple(() =>
		{
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(inheritance))!.MemberName, Is.EqualTo(nameof(CspDefinition.Inheritance)));
			Assert.That(Assert.ThrowsAsync<CspDefinitionValidationException>(() => SaveAsync(appliesTo))!.MemberName, Is.EqualTo(nameof(CspDefinition.AppliesTo)));
		});
	}

	// ── Lookups ──────────────────────────────────────────────────────────────

	[Test]
	public async Task GetCspDefinitionForNode_ReturnsThePolicyStoredForExactlyThatCulture()
	{
		var all = await SaveAsync(NewPolicy(_home, "all.example.com"));
		var french = await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));

		Assert.Multiple(async () =>
		{
			Assert.That((await _cspService.GetCspDefinitionForNodeAsync(_home, CancellationToken.None))?.Id, Is.EqualTo(all.Id));
			Assert.That((await _cspService.GetCspDefinitionForNodeAsync(_home, null, CancellationToken.None))?.Id, Is.EqualTo(all.Id));
			Assert.That((await _cspService.GetCspDefinitionForNodeAsync(_home, "fr-fr", CancellationToken.None))?.Id, Is.EqualTo(french.Id));
			Assert.That(SourcesOf(await _cspService.GetCspDefinitionForNodeAsync(_home, "fr-FR", CancellationToken.None)), Is.EqualTo(new[] { "fr.example.com" }));
			Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_home, "fr-CA", CancellationToken.None), Is.Null, "no fallback is applied");
			Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_page, CancellationToken.None), Is.Null, "nor the policy of an ancestor");
		});
	}

	[Test]
	public async Task GetCachedCspDefinitionForNode_ReturnsThePolicyForAllCulturesOnly()
	{
		await SaveAsync(NewPolicy(_home, "fr.example.com", culture: "fr-FR"));
		Assert.That(await _cspService.GetCachedCspDefinitionForNodeAsync(_home, CancellationToken.None), Is.Null);

		var all = await SaveAsync(NewPolicy(_home, "all.example.com"));

		Assert.That((await _cspService.GetCachedCspDefinitionForNodeAsync(_home, CancellationToken.None))?.Id, Is.EqualTo(all.Id));
	}

	[Test]
	public async Task ContentNodeExists_IsTrueForAnyNodeThatIsNotInTheRecycleBin()
	{
		Assert.Multiple(async () =>
		{
			Assert.That(await _cspService.ContentNodeExistsAsync(_page, CancellationToken.None), Is.True);
			Assert.That(await _cspService.ContentNodeExistsAsync(Trashed.Key, CancellationToken.None), Is.False);
			Assert.That(await _cspService.ContentNodeExistsAsync(Guid.NewGuid(), CancellationToken.None), Is.False);
		});
	}
}
