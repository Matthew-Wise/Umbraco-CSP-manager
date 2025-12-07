using System.Linq.Expressions;
using System.Net;
using System.Text.Json.JsonDiffPatch.Nunit;
using Umbraco.Community.CSPManager.Controllers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;

internal class GetDefinitionTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector => x => x.GetDefinition(true, default);

	[Test]
	public async Task GetDefinition_WithOutSectionAcess_Returns_Forbidden()
	{

		await AuthenticateClientAsync(Client, "get-test@test.com", "1234567890", UmbConstants.Security.EditorGroupKey);
		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task GetDefinition_WithSectionAces_Returns_Ok()
	{
		var userGroup = await CreateCspUserGroupAsync();
		await AuthenticateClientAsync(Client, "get-valid@test.com", "1234567890", userGroup.Key);

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}

	[Test]
	public async Task GetDefinition_Returns_KnownDefaultBackOfficeCSP()
	{
		await AuthenticateClientAsync(Client, "get-admin@example.com", "1234567890", true);

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		var actual = await result.Content.ReadAsStringAsync();
		var expected = """
			{
			  "id": "00000000-0000-0000-0000-000000000000",
			  "enabled": false,
			  "reportOnly": false,
			  "isBackOffice": true,
			  "reportingDirective": null,
			  "reportUri": null,
			  "upgradeInsecureRequests": false,
			  "sources": [
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "'self'",
			      "directives": [
			        "default-src",
			        "script-src",
			        "style-src",
			        "img-src",
			        "font-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "'unsafe-eval'",
			      "directives": [
			        "script-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "'unsafe-inline'",
			      "directives": [
			        "script-src",
			        "style-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "dashboard.umbraco.com",
			      "directives": [
			        "img-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "data:",
			      "directives": [
			        "img-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "marketplace.umbraco.com",
			      "directives": [
			        "default-src"
			      ]
			    },
			    {
			      "definitionId": "00000000-0000-0000-0000-000000000000",
			      "source": "our.umbraco.com",
			      "directives": [
			        "default-src",
			        "img-src"
			      ]
			    }
			  ]
			}
			""";

		JsonAssert.AreEqual(expected, actual, true);
	}
}