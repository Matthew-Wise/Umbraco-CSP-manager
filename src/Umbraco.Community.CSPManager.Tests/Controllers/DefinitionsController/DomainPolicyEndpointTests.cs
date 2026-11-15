using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
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
			("GET Definitions?contentKey", () => Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, CancellationToken.None)))),
			("GET Definitions/{id}", () => Client.GetAsync(DefinitionsUrl(x => x.GetDefinitionById(policy.Id, CancellationToken.None)))),
			("GET Definitions/node-policies", () => Client.GetAsync(DefinitionsUrl(x => x.GetNodePolicies(CancellationToken.None)))),
			("POST Definitions/create-from-frontend", () => Client.PostAsync(DefinitionsUrl(x => x.CreateFromFrontend(_siteB.Key, CancellationToken.None)), null)),
			("POST Definitions/save (create)", () => Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(NewPolicyBody(_siteB.Key)))),
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

		var response = await Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(NewPolicyBody(_siteA.Key)));

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

		var response = await Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(NewPolicyBody(_siteA.Key, Guid.NewGuid())));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		Assert.That(await _cspService.GetCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None), Is.Null);
	}

	[Test]
	public async Task Save_SecondPolicyForTheSameDomain_ReturnsBadRequest()
	{
		await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		await AuthenticateAsAdminAsync();

		var response = await Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(NewPolicyBody(_siteA.Key)));

		Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		Assert.That(await response.Content.ReadAsStringAsync(), Does.Contain("A policy already exists for this content node."));
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

		var response = await Client.PostAsync(DefinitionsUrl(x => x.CreateFromFrontend(_siteA.Key, CancellationToken.None)), null);

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
		var response = await Client.PostAsync(DefinitionsUrl(x => x.SaveDefinition(null!, CancellationToken.None)), JsonContent.Create(body));

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
		var byDomain = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteA.Key, CancellationToken.None)));

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
		var byDomain = await Client.GetAsync(DefinitionsUrl(x => x.GetDefinition(false, _siteB.Key, CancellationToken.None)));

		Assert.Multiple(() =>
		{
			Assert.That(byId.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
			Assert.That(byDomain.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		});
	}

	[Test]
	public async Task GetDomainPolicies_ListsPoliciesAndFlagsOrphans()
	{
		var policyA = await _cspService.CreateCspDefinitionForNodeAsync(_siteA.Key, CancellationToken.None);
		var policyB = await _cspService.CreateCspDefinitionForNodeAsync(_siteB.Key, CancellationToken.None);
		// Remove the hostname from site B: its policy is kept but orphaned.
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
			// The node still exists, so the orphan keeps its name.
			Assert.That(b.ContentName, Is.EqualTo("Site B"));
			Assert.That(b.IsOrphaned, Is.True);
			Assert.That(b.ContentKey, Is.EqualTo(_siteB.Key));
		});
	}

	[Test]
	public async Task GetDomainPoliciesAndDomains_TreatANodeInTheRecycleBinAsHavingNoHostname()
	{
		var policyB = await _cspService.CreateCspDefinitionForNodeAsync(_siteB.Key, CancellationToken.None);
		// Umbraco keeps the domains of a trashed node, but never routes a request to it.
		GetRequiredService<IContentService>().MoveToRecycleBin(_siteB);
		await AuthenticateAsAdminAsync();

		var policies = await (await Client.GetAsync(Url)).Content.ReadFromJsonAsync<List<CspApiNodePolicy>>(JsonSerializerOptions);
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
