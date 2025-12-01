using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Models.Api;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;

internal class SaveDefinitionsAuthorizationTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector => x => x.SaveDefinition(default!, default);

	[Test]
	public async Task SaveDefinition_WithOutSectionAcess_Returns_Forbidden()
	{
		await AuthenticateClientAsync(Client, "auth-test@test.com", "1234567890", UmbConstants.Security.EditorGroupKey);
		var definition = JsonContent.Create<CspApiDefinition>(new()
		{
			Id = Guid.Empty
		});

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, definition);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task SaveDefinition_WithSectionAcessAndValidObject_Returns_Ok()
	{
		var userGroup = await CreateCspUserGroupAsync();

		await AuthenticateClientAsync(Client, "auth-valid@test.com", "1234567890", userGroup.Key);
		var definition = JsonContent.Create<CspApiDefinition>(new()
		{
			Id = Constants.DefaultBackofficeId,
		});

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, definition);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}
}