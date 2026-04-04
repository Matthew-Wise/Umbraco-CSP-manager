using System.Linq.Expressions;
using System.Net;
using System.Text.Json;
using DirectivesControllerType = Umbraco.Community.CSPManager.Controllers.DirectivesController;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DirectivesController;

internal class GetDirectivesTests : CspManagementApiTest<DirectivesControllerType>
{
	protected override Expression<Func<DirectivesControllerType, object>> MethodSelector
		=> x => x.GetCspDirectiveOptions(default);

	[Test]
	public async Task GetDirectives_WithoutSectionAccess_ReturnsForbidden()
	{
		await AuthenticateClientAsync(Client, "directives-noaccess@test.com", "1234567890", UmbConstants.Security.EditorGroupKey);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task GetDirectives_WithSectionAccess_ReturnsOkWithAllDirectives()
	{
		var userGroup = await CreateCspUserGroupAsync();
		await AuthenticateClientAsync(Client, "directives-access@test.com", "1234567890", userGroup.Key);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

		var content = await result.Content.ReadAsStringAsync();
		var directives = JsonSerializer.Deserialize<List<string>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		Assert.That(directives, Is.Not.Null);
		Assert.That(directives, Is.EquivalentTo(Constants.AllDirectives));
	}
}