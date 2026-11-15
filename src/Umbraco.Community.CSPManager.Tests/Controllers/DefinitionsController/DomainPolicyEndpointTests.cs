using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;
using Umbraco.Community.CSPManager.Tests.Helpers;
using DefinitionsControllerType = Umbraco.Community.CSPManager.Controllers.DefinitionsController;
using NodesControllerType = Umbraco.Community.CSPManager.Controllers.CspNodesController;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitionsController;

/// <summary>
/// The node policy endpoints: authorization on every one of them, the create/save/delete
/// rules (including the id-hijack cases), and the 400/404 paths.
/// </summary>
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
internal class DomainPolicyEndpointTests : CspManagementApiTest<DefinitionsControllerType>
{
	protected override Expression<Func<DefinitionsControllerType, object>> MethodSelector { get; set; } = x => x.GetNodePolicies(CancellationToken.None);

	private static int _userCounter;

	private ICspService _cspService;
	private IContent _siteA;
	private IContent _siteB;
	// A page below site A, without a hostname of its own.
	private IContent _pageA;

	[SetUp]
	public async Task SetUpDomains()
	{
		_cspService = GetRequiredService<ICspService>();

		var contentType = ContentTypeBuilder.CreateSimpleContentType("site", "Site");
		contentType.AllowedAsRoot = true;
		var contentTypeResult = await GetRequiredService<IContentTypeService>().CreateAsync(contentType, UmbConstants.Security.SuperUserKey);
		Assert.That(contentTypeResult.Success, Is.True);

		var contentService = GetRequiredService<IContentService>();
		_siteA = ContentBuilder.CreateSimpleContent(contentType, "Site A");
		contentService.Save(_siteA);
		_siteB = ContentBuilder.CreateSimpleContent(contentType, "Site B");
		contentService.Save(_siteB);
		_pageA = ContentBuilder.CreateSimpleContent(contentType, "Page A", _siteA.Id);
		contentService.Save(_pageA);

		// A running site keeps Umbraco's in-memory navigation structure up to date; the test host doesn't.
		await GetRequiredService<IDocumentNavigationManagementService>().RebuildAsync();

		await CspTestLanguageHelper.CreateLanguageAsync(GetRequiredService<ILanguageService>(), "fr-FR");

		var domainService = GetRequiredService<IDomainService>();
		await CspTestDomainHelper.AssignDomainsAsync(domainService, _siteA.Key, ["a.example.com"]);
		await CspTestDomainHelper.AssignDomainsAsync(domainService, _siteB.Key, ["b.example.com"]);
	}

	private async Task AuthenticateAsAdminAsync()
		=> await AuthenticateClientAsync(Client, $"domain-admin{Interlocked.Increment(ref _userCounter)}@example.com", UserPassword, true);

	private async Task AuthenticateAsEditorAsync()
		=> await AuthenticateClientAsync(Client, $"domain-editor{Interlocked.Increment(ref _userCounter)}@example.com", UserPassword, UmbConstants.Security.EditorGroupKey);

	private string DefinitionsUrl(Expression<Func<DefinitionsControllerType, object>> selector) => GetManagementApiUrl(selector);

	private async Task<CspApiDefinition> ReadDefinitionAsync(HttpResponseMessage response)
		=> await response.Content.ReadFromJsonAsync<CspApiDefinition>(JsonSerializerOptions);

	private Task<HttpResponseMessage> PostSaveAsync(CspApiDefinition body)
		=> Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(body));

	private async Task<List<CspApiNodePolicy>> GetNodePoliciesAsync()
		=> await (await Client.GetAsync(Url)).Content.ReadFromJsonAsync<List<CspApiNodePolicy>>(JsonSerializerOptions);

	// A complete policy for French, for the node and the pages below it.
	private Task<CspDefinition> CreateFrenchPolicyAsync(Guid contentKey)
		=> _cspService.CreateCspDefinitionForNodeAsync(contentKey, "fr-FR", CspPolicyInheritance.Replace, CspPolicyScope.Descendants, CancellationToken.None);

	private static CspApiDefinition NewPolicyBody(Guid contentKey, Guid? id = null) => new()
	{
		Id = id ?? Guid.Empty,
		ContentKey = contentKey,
		Enabled = true,
		Sources = [new CspApiDefinitionSource { Source = "'self'", Directives = [Constants.Directives.DefaultSource] }]
	};

	// ── Authorization ────────────────────────────────────────────────────────

	[Test]
	public async Task EveryDomainPolicyEndpoint_WithoutSectionAccess_ReturnsForbidden()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsEditorAsync();

		var requests = new (string Name, Func<Task<HttpResponseMessage>> Send)[]
		{
			("GET Definitions?contentKey", () => Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, null, CancellationToken.None)))),
			("GET Definitions/{id}", () => Client.GetAsync(DefinitionsUrl(x => x.GetDefinitionById(policy.Id, CancellationToken.None)))),
			("GET Definitions/effective", () => Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_siteA.Key, null, CancellationToken.None)))),
			("GET Definitions/node-policies", () => Client.GetAsync(DefinitionsUrl(x => x.GetNodePolicies(CancellationToken.None)))),
			("POST Definitions/create-from-frontend", () => Client.PostAsync(DefinitionsUrl(x => x.CreateFromFrontend(_siteB.Key, null, CspPolicyInheritance.Replace, CspPolicyScope.Descendants, CancellationToken.None)), null)),
			("POST Definitions/save (create)", () => PostSaveAsync(NewPolicyBody(_siteB.Key))),
			("DELETE Definitions/{id}", () => Client.DeleteAsync(DefinitionsUrl(x => x.DeleteDefinition(policy.Id, CancellationToken.None)))),
			("GET Nodes", () => Client.GetAsync(GetManagementApiUrl<NodesControllerType>(x => x.GetNodes(CancellationToken.None)))),
		};

		foreach (var (name, send) in requests)
		{
			var response = await send();
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), name);
		}

		Assert.That(await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None), Is.Not.Null, "the forbidden delete must not have run");
		Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_siteB.Key, CancellationToken.None), Is.Null, "the forbidden creates must not have run");
	}

	// ── Create ───────────────────────────────────────────────────────────────

	[Test]
	public async Task Save_WithEmptyIdAndContentKey_CreatesWithAServerAssignedId()
	{
		await AuthenticateAsAdminAsync();

		var response = await PostSaveAsync(NewPolicyBody(_siteA.Key));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var created = await ReadDefinitionAsync(response);
		Assert.Multiple(() =>
		{
			Assert.That(created.Id, Is.Not.EqualTo(Guid.Empty));
			Assert.That(created.ContentKey, Is.EqualTo(_siteA.Key));
			Assert.That(created.ContentName, Is.EqualTo("Site A"));
			Assert.That(created.Domains.Select(d => d.Name), Is.EqualTo(new[] { "a.example.com" }));
			Assert.That(created.DisabledNodePolicyBehavior, Is.EqualTo(DisabledNodePolicyBehavior.FallbackToGlobal));
		});
		var stored = await _cspService.GetCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		Assert.That(stored?.Id, Is.EqualTo(created.Id));
	}

	[Test]
	public async Task Save_WithAClientChosenIdThatDoesNotExist_ReturnsNotFound()
	{
		await AuthenticateAsAdminAsync();

		var response = await PostSaveAsync(NewPolicyBody(_siteA.Key, Guid.NewGuid()));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None), Is.Null);
	}

	[Test]
	public async Task Save_SecondPolicyForTheSameDomain_ReturnsBadRequest()
	{
		await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var response = await PostSaveAsync(NewPolicyBody(_siteA.Key));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("A policy already exists for this content node and culture."));
		Assert.That(await _cspService.GetAllNodePoliciesAsync(CancellationToken.None), Has.Count.EqualTo(1));
	}

	[Test]
	public async Task CreateFromFrontend_CopiesTheFrontendPolicy()
	{
		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "frontend.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var response = await Client.PostAsync(DefinitionsUrl(x => x.CreateFromFrontend(_siteA.Key, null, CspPolicyInheritance.Replace, CspPolicyScope.Descendants, CancellationToken.None)), null);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var created = await ReadDefinitionAsync(response);
		Assert.Multiple(() =>
		{
			Assert.That(created.Id, Is.Not.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(created.ContentKey, Is.EqualTo(_siteA.Key));
			Assert.That(created.Sources.Select(s => s.Source), Is.EqualTo(new[] { "frontend.example.com" }));
		});
	}

	// ── Update ───────────────────────────────────────────────────────────────

	[Test]
	public async Task Save_ExistingPolicyWithItsOwnIdAndDomain_ReturnsOk()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var body = NewPolicyBody(_siteA.Key, policy.Id);
		body.Enabled = false;
		var response = await PostSaveAsync(body);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var stored = await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None);
		Assert.That(stored.Enabled, Is.False);
	}

	// ── Read ─────────────────────────────────────────────────────────────────

	[Test]
	public async Task GetDefinitionById_And_GetDefinitionForDomain_ReturnTheDomainPolicy()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var byId = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinitionById(policy.Id, CancellationToken.None)));
		var byDomain = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, null, CancellationToken.None)));

		Assert.Multiple(async () =>
		{
			Assert.That(byId.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(byDomain.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That((await ReadDefinitionAsync(byId)).Domains.Select(d => d.Name), Is.EqualTo(new[] { "a.example.com" }));
			Assert.That((await ReadDefinitionAsync(byDomain)).Id, Is.EqualTo(policy.Id));
		});
	}

	[Test]
	public async Task GetDefinitionById_And_GetDefinitionForDomain_WhenMissing_ReturnNotFound()
	{
		await AuthenticateAsAdminAsync();

		var byId = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinitionById(Guid.NewGuid(), CancellationToken.None)));
		var byDomain = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteB.Key, null, CancellationToken.None)));

		Assert.Multiple(() =>
		{
			Assert.That(byId.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
			Assert.That(byDomain.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		});
	}

	[Test]
	public async Task GetDomainPolicies_ListsPolicies_AndANodeNeedsNoHostname()
	{
		var policyA = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		var policyB = await _cspService.CreateCspDefinitionForNodeAsync(_siteB.Key, CancellationToken.None);
		// Remove the hostname from site B: any content node can have a policy, so it stays in use.
		await CspTestDomainHelper.AssignDomainsAsync(GetRequiredService<IDomainService>(), _siteB.Key, []);
		await AuthenticateAsAdminAsync();

		var response = await Client.GetAsync(Url);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var policies = await response.Content.ReadFromJsonAsync<List<CspApiNodePolicy>>(JsonSerializerOptions);
		Assert.That(policies, Has.Count.EqualTo(2));
		var a = policies.Single(p => p.Id == policyA.Id);
		var b = policies.Single(p => p.Id == policyB.Id);
		Assert.Multiple(() =>
		{
			Assert.That(a.ContentName, Is.EqualTo("Site A"));
			Assert.That(a.IsOrphaned, Is.False);
			Assert.That(a.Culture, Is.Null);
			Assert.That(a.Inheritance, Is.EqualTo(CspPolicyInheritance.Replace));
			Assert.That(a.AppliesTo, Is.EqualTo(CspPolicyScope.Descendants));
			Assert.That(b.ContentName, Is.EqualTo("Site B"));
			Assert.That(b.IsOrphaned, Is.False);
			Assert.That(b.ContentKey, Is.EqualTo(_siteB.Key));
		});
	}

	[Test]
	public async Task GetDomainPolicies_ListsThePoliciesOfANodePerCulture_AllCulturesFirst()
	{
		var french = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, "fr-FR", CspPolicyInheritance.Extend, CspPolicyScope.NodeOnly, CancellationToken.None);
		var all = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var policies = await GetNodePoliciesAsync();

		Assert.Multiple(() =>
		{
			Assert.That(policies.Select(p => p.Id), Is.EqualTo(new[] { all.Id, french.Id }));
			Assert.That(policies[1].Culture, Is.EqualTo("fr-FR"));
			Assert.That(policies[1].Inheritance, Is.EqualTo(CspPolicyInheritance.Extend));
			Assert.That(policies[1].AppliesTo, Is.EqualTo(CspPolicyScope.NodeOnly));
			Assert.That(policies.Select(p => p.IsOrphaned), Is.All.False);
		});
	}

	// A policy for a culture that is no longer a language applies to nothing.
	[Test]
	public async Task GetDomainPolicies_FlagsAPolicyWhoseLanguageWasDeletedAsOrphaned()
	{
		var french = await CreateFrenchPolicyAsync(_siteA.Key);
		var all = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		var deleted = await GetRequiredService<ILanguageService>().DeleteAsync("fr-FR", UmbConstants.Security.SuperUserKey);
		Assert.That(deleted.Success, Is.True, deleted.Status.ToString());
		await AuthenticateAsAdminAsync();

		var policies = await GetNodePoliciesAsync();

		Assert.Multiple(() =>
		{
			Assert.That(policies.Single(p => p.Id == french.Id).IsOrphaned, Is.True);
			Assert.That(policies.Single(p => p.Id == all.Id).IsOrphaned, Is.False);
		});
	}

	// ── Any node, cultures and inheritance ───────────────────────────────────

	[Test]
	public async Task Save_NewPolicyForAPageWithoutAHostname_ReturnsOk()
	{
		await AuthenticateAsAdminAsync();
		var body = NewPolicyBody(_pageA.Key);
		body.Inheritance = CspPolicyInheritance.Extend;
		body.AppliesTo = CspPolicyScope.NodeOnly;

		var response = await PostSaveAsync(body);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var created = await ReadDefinitionAsync(response);
		Assert.Multiple(() =>
		{
			Assert.That(created.ContentKey, Is.EqualTo(_pageA.Key));
			Assert.That(created.ContentName, Is.EqualTo("Page A"));
			Assert.That(created.Domains, Is.Empty);
			Assert.That(created.Culture, Is.Null);
			Assert.That(created.Inheritance, Is.EqualTo(CspPolicyInheritance.Extend));
			Assert.That(created.AppliesTo, Is.EqualTo(CspPolicyScope.NodeOnly));
		});
	}

	[Test]
	public async Task Save_NewPolicyForACulture_ReturnsOk_AndForAnUnknownCulture_ReturnsBadRequest()
	{
		await AuthenticateAsAdminAsync();
		var french = NewPolicyBody(_siteA.Key);
		french.Culture = "fr-FR";
		var dutch = NewPolicyBody(_siteA.Key);
		dutch.Culture = "nl-NL";

		var created = await PostSaveAsync(french);
		var rejected = await PostSaveAsync(dutch);

		Assert.Multiple(async () =>
		{
			Assert.That(created.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That((await ReadDefinitionAsync(created)).Culture, Is.EqualTo("fr-FR"));
			Assert.That(rejected.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			// The apostrophe in "the site's languages" is escaped in the JSON body.
			Assert.That(await rejected.Content.ReadAsStringAsync(), Does.Contain("$.culture").And.Contain("The culture is not one of the site"));
		});
	}

	[Test]
	public async Task Save_GlobalPolicyWithACultureOrInheritance_ReturnsBadRequest()
	{
		await AuthenticateAsAdminAsync();
		var withCulture = new CspApiDefinition { Id = Constants.DefaultFrontEndId, Culture = "fr-FR" };
		var extending = new CspApiDefinition { Id = Constants.DefaultFrontEndId, Inheritance = CspPolicyInheritance.Extend };

		foreach (var body in new[] { withCulture, extending })
		{
			var response = await PostSaveAsync(body);
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}
	}

	[Test]
	public async Task CreateFromFrontend_ForACultureAsAnExtendingPolicyForOnePage_CreatesIt()
	{
		await AuthenticateAsAdminAsync();

		var response = await Client.PostAsync(
			DefinitionsUrl(x => x.CreateFromFrontend(_pageA.Key, "fr-FR", CspPolicyInheritance.Extend, CspPolicyScope.NodeOnly, CancellationToken.None)),
			null);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var created = await ReadDefinitionAsync(response);
		Assert.Multiple(() =>
		{
			Assert.That(created.ContentKey, Is.EqualTo(_pageA.Key));
			Assert.That(created.Culture, Is.EqualTo("fr-FR"));
			Assert.That(created.Inheritance, Is.EqualTo(CspPolicyInheritance.Extend));
			Assert.That(created.AppliesTo, Is.EqualTo(CspPolicyScope.NodeOnly));
			Assert.That(created.Enabled, Is.True);
			Assert.That(created.Sources, Is.Empty);
		});
	}

	[Test]
	public async Task CreateFromFrontend_ForAnUnknownCulture_ReturnsBadRequest()
	{
		await AuthenticateAsAdminAsync();

		var response = await Client.PostAsync(
			DefinitionsUrl(x => x.CreateFromFrontend(_siteA.Key, "nl-NL", CspPolicyInheritance.Replace, CspPolicyScope.Descendants, CancellationToken.None)),
			null);

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		Assert.That(await _cspService.GetAllNodePoliciesAsync(CancellationToken.None), Is.Empty);
	}

	[Test]
	public async Task GetDefinition_ForANodeAndCulture_ReturnsThePolicyStoredForThatCulture()
	{
		var all = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		var french = await CreateFrenchPolicyAsync(_siteA.Key);
		await AuthenticateAsAdminAsync();

		var forAll = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, null, CancellationToken.None)));
		var forFrench = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, "fr-FR", CancellationToken.None)));
		var forDanish = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, "da-DK", CancellationToken.None)));

		Assert.Multiple(async () =>
		{
			Assert.That((await ReadDefinitionAsync(forAll)).Id, Is.EqualTo(all.Id));
			Assert.That((await ReadDefinitionAsync(forFrench)).Id, Is.EqualTo(french.Id));
			Assert.That(forDanish.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "no fallback is applied");
		});
	}

	[Test]
	public async Task GetEffectiveDefinition_ReturnsThePolicyARequestForThePageGets()
	{
		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			Id = Constants.DefaultFrontEndId,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "frontend.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		var siteA = await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			ContentKey = _siteA.Key,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "site-a.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		var pageA = await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			ContentKey = _pageA.Key,
			Culture = "fr-FR",
			Inheritance = CspPolicyInheritance.Extend,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "maps.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var inherited = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_pageA.Key, null, CancellationToken.None))));
		var extended = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_pageA.Key, "fr-FR", CancellationToken.None))));
		var global = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_siteB.Key, null, CancellationToken.None))));
		var missing = await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(Guid.NewGuid(), null, CancellationToken.None)));

		Assert.Multiple(() =>
		{
			Assert.That(inherited.Id, Is.EqualTo(siteA.Id));
			Assert.That(inherited.ContentName, Is.EqualTo("Site A"));
			Assert.That(inherited.Sources.Select(s => s.Source), Is.EqualTo(new[] { "site-a.example.com" }));

			Assert.That(extended.Id, Is.EqualTo(Guid.Empty), "a combined result is not a stored policy");
			Assert.That(extended.Sources.Select(s => s.DefinitionId), Is.All.EqualTo(Guid.Empty));
			Assert.That(extended.ContentKey, Is.EqualTo(pageA.ContentKey));
			Assert.That(extended.Culture, Is.EqualTo("fr-FR"));
			Assert.That(extended.ContentName, Is.EqualTo("Page A"));
			Assert.That(extended.Sources.Select(s => s.Source), Is.EqualTo(new[] { "site-a.example.com", "maps.example.com" }));

			Assert.That(global.Id, Is.EqualTo(Constants.DefaultFrontEndId));
			Assert.That(global.ContentKey, Is.Null);
			Assert.That(global.Sources.Select(s => s.Source), Is.EqualTo(new[] { "frontend.example.com" }));

			Assert.That(missing.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		});
	}

	// A routed request always has a culture: without a hostname that sets one, the default language.
	[Test]
	public async Task GetEffectiveDefinition_WithoutACulture_AnswersForTheDefaultLanguage()
	{
		var defaultLanguage = await GetRequiredService<ILanguageService>().GetDefaultIsoCodeAsync();
		var forDefaultLanguage = await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			ContentKey = _siteA.Key,
			Culture = defaultLanguage,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "default-language.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		var french = await CreateFrenchPolicyAsync(_siteA.Key);
		await AuthenticateAsAdminAsync();

		var withoutCulture = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_pageA.Key, null, CancellationToken.None))));
		var padded = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_pageA.Key, " fr-fr ", CancellationToken.None))));

		Assert.Multiple(() =>
		{
			Assert.That(withoutCulture.Id, Is.EqualTo(forDefaultLanguage.Id));
			Assert.That(padded.Id, Is.EqualTo(french.Id), "the culture is trimmed and matched whatever its case");
		});
	}

	// The combined result has the sources of the whole chain. It must not be possible to post it
	// back and so copy everything the extending policy inherits into that policy.
	[Test]
	public async Task GetEffectiveDefinition_CombinedResult_CannotBeSavedOverTheExtendingPolicy()
	{
		await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			ContentKey = _siteA.Key,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "site-a.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		var extending = await _cspService.SaveCspDefinitionAsync(new CspDefinition
		{
			ContentKey = _pageA.Key,
			Inheritance = CspPolicyInheritance.Extend,
			Enabled = true,
			Sources = [new CspDefinitionSource { Source = "maps.example.com", Directives = [Constants.Directives.ScriptSource] }]
		}, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var effective = await ReadDefinitionAsync(await Client.GetAsync(DefinitionsUrl(x => x.GetEffectiveDefinition(_pageA.Key, null, CancellationToken.None))));
		var response = await PostSaveAsync(effective);

		var stored = await _cspService.GetCspDefinitionAsync(extending.Id, CancellationToken.None);
		Assert.Multiple(() =>
		{
			Assert.That(effective.Sources.Select(s => s.Source), Is.EqualTo(new[] { "site-a.example.com", "maps.example.com" }));
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
			Assert.That(stored!.Sources.Select(s => s.Source), Is.EqualTo(new[] { "maps.example.com" }));
		});
	}

	// The Nodes list says whether a site has a policy for all cultures; a policy for one culture
	// next to it (or on its own) must not break the list.
	[Test]
	public async Task GetDomains_WithPoliciesPerCulture_ReportsThePolicyForAllCultures()
	{
		await CreateFrenchPolicyAsync(_siteA.Key);
		var all = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await CreateFrenchPolicyAsync(_siteB.Key);
		await AuthenticateAsAdminAsync();

		var response = await Client.GetAsync(GetManagementApiUrl<NodesControllerType>(x => x.GetNodes(CancellationToken.None)));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var nodes = await response.Content.ReadFromJsonAsync<List<CspApiNodeInfo>>(JsonSerializerOptions);
		Assert.Multiple(() =>
		{
			Assert.That(nodes.Single(n => n.ContentKey == _siteA.Key).CspDefinitionId, Is.EqualTo(all.Id));
			Assert.That(nodes.Single(n => n.ContentKey == _siteB.Key).HasCspPolicy, Is.False);
		});
	}

	[Test]
	public async Task GetDomainPoliciesAndDomains_TreatANodeInTheRecycleBinAsHavingNoHostname()
	{
		var policyB = await _cspService.CreateCspDefinitionForNodeAsync(_siteB.Key, CancellationToken.None);
		// Umbraco keeps the domains of a trashed node, but never routes a request to it.
		GetRequiredService<IContentService>().MoveToRecycleBin(_siteB);
		await AuthenticateAsAdminAsync();

		var policies = await GetNodePoliciesAsync();
		var nodes = await (await Client.GetAsync(GetManagementApiUrl<NodesControllerType>(x => x.GetNodes(CancellationToken.None))))
			.Content.ReadFromJsonAsync<List<CspApiNodeInfo>>(JsonSerializerOptions);

		var b = policies.Single(p => p.Id == policyB.Id);
		Assert.Multiple(() =>
		{
			Assert.That(b.IsOrphaned, Is.True);
			Assert.That(b.ContentName, Is.EqualTo("Site B"));
			Assert.That(nodes.Select(n => n.ContentKey), Is.EqualTo(new[] { _siteA.Key }));
		});
	}

	[Test]
	public async Task GetDomains_ListsNodesWithTheirHostnamesAndPolicy()
	{
		var domainService = GetRequiredService<IDomainService>();
		// A multilingual site: two hostnames on site A, listed once, in Umbraco's sort order.
		await CspTestDomainHelper.AssignDomainsAsync(domainService, _siteA.Key, ["a.example.com/nl", "a.example.com/fr"]);
		var policyA = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var response = await Client.GetAsync(GetManagementApiUrl<NodesControllerType>(x => x.GetNodes(CancellationToken.None)));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
		var nodes = await response.Content.ReadFromJsonAsync<List<CspApiNodeInfo>>(JsonSerializerOptions);
		Assert.That(nodes, Has.Count.EqualTo(2));
		var a = nodes.Single(n => n.ContentKey == _siteA.Key);
		var b = nodes.Single(n => n.ContentKey == _siteB.Key);
		Assert.Multiple(() =>
		{
			Assert.That(a.ContentName, Is.EqualTo("Site A"));
			Assert.That(a.Domains.Select(d => d.Name), Is.EqualTo(new[] { "a.example.com/nl", "a.example.com/fr" }));
			Assert.That(a.Domains.Select(d => d.Culture), Is.All.EqualTo("en-US"));
			Assert.That(a.HasCspPolicy, Is.True);
			Assert.That(a.CspDefinitionId, Is.EqualTo(policyA.Id));
			Assert.That(b.ContentName, Is.EqualTo("Site B"));
			Assert.That(b.HasCspPolicy, Is.False);
			Assert.That(b.CspDefinitionId, Is.Null);
		});
	}

	// Renaming a hostname no longer orphans the policy: it belongs to the node.
	[Test]
	public async Task GetDomainPolicies_AfterAHostnameRename_StillListsThePolicyUnderTheNewHostname()
	{
		var policyA = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await CspTestDomainHelper.AssignDomainsAsync(GetRequiredService<IDomainService>(), _siteA.Key, ["www.a.example.com"]);
		await AuthenticateAsAdminAsync();

		var response = await Client.GetAsync(Url);

		var policy = (await response.Content.ReadFromJsonAsync<List<CspApiNodePolicy>>(JsonSerializerOptions)).Single();
		Assert.Multiple(() =>
		{
			Assert.That(policy.Id, Is.EqualTo(policyA.Id));
			Assert.That(policy.IsOrphaned, Is.False);
			Assert.That(policy.ContentName, Is.EqualTo("Site A"));
		});
	}

	// ── Delete ───────────────────────────────────────────────────────────────

	[Test]
	public async Task Delete_DomainPolicy_ReturnsOkThenNotFound()
	{
		var policy = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var first = await Client.DeleteAsync(DefinitionsUrl(x => x.DeleteDefinition(policy.Id, CancellationToken.None)));
		var second = await Client.DeleteAsync(DefinitionsUrl(x => x.DeleteDefinition(policy.Id, CancellationToken.None)));

		Assert.Multiple(async () =>
		{
			Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
			Assert.That(await _cspService.GetCspDefinitionAsync(policy.Id, CancellationToken.None), Is.Null);
		});
	}

	// The controller skips its 404 check for the global ids, so the service's refusal must surface as 400.
	[Test]
	public async Task Delete_GlobalPolicy_ReturnsBadRequest()
	{
		var id = Constants.DefaultFrontEndId;
		await _cspService.SaveCspDefinitionAsync(new CspDefinition { Id = Constants.DefaultFrontEndId }, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var response = await Client.DeleteAsync(DefinitionsUrl(x => x.DeleteDefinition(id, CancellationToken.None)));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		Assert.That(await _cspService.GetCspDefinitionAsync(id, CancellationToken.None), Is.Not.Null);
	}

}
