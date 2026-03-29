using System.Linq.Expressions;
using System.Net;
using Umbraco.Community.CSPManager.Controllers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DomainsController;

internal class GetDomainsTests : CspManagementApiTest<CspDomainsController>
{
	protected override Expression<Func<CspDomainsController, object>> MethodSelector =>
		x => x.GetDomains(default);

	[Test]
	public async Task GetDomains_WithoutSectionAccess_ReturnsForbidden()
	{
		await AuthenticateClientAsync(Client, "domains-test@test.com", "1234567890", UmbConstants.Security.EditorGroupKey);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task GetDomains_WithSectionAccess_ReturnsOk()
	{
		var userGroup = await CreateCspUserGroupAsync();
		await AuthenticateClientAsync(Client, "domains-valid@test.com", "1234567890", userGroup.Key);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}

	[Test]
	public async Task GetDomains_WithNoDomains_ReturnsEmptyArray()
	{
		await AuthenticateClientAsync(Client, "domains-admin@example.com", "1234567890", true);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		var content = await result.Content.ReadAsStringAsync();
		Assert.That(content, Is.EqualTo("[]"));
	}
}
