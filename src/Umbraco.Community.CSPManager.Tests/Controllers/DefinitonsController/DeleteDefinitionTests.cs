using System.Linq.Expressions;
using System.Net;
using Umbraco.Community.CSPManager.Controllers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;

internal class DeleteDefinitionTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector =>
		x => x.DeleteDefinition(Constants.DefaultBackofficeId, default);

	[Test]
	public async Task DeleteDefinition_WithoutSectionAccess_ReturnsForbidden()
	{
		await AuthenticateClientAsync(Client, "delete-test@test.com", "1234567890", UmbConstants.Security.EditorGroupKey);
		var url = GetManagementApiUrl<DefinitionsController>(x => x.DeleteDefinition(Constants.DefaultBackofficeId, default));
		var result = await Client.DeleteAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task DeleteDefinition_WithGlobalBackofficeId_ReturnsBadRequest()
	{
		await AuthenticateClientAsync(Client, "delete-admin-bo@example.com", "1234567890", true);
		var url = GetManagementApiUrl<DefinitionsController>(x => x.DeleteDefinition(Constants.DefaultBackofficeId, default));
		var result = await Client.DeleteAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
	}

	[Test]
	public async Task DeleteDefinition_WithGlobalFrontEndId_ReturnsBadRequest()
	{
		await AuthenticateClientAsync(Client, "delete-admin-fe@example.com", "1234567890", true);
		var url = GetManagementApiUrl<DefinitionsController>(x => x.DeleteDefinition(Constants.DefaultFrontEndId, default));
		var result = await Client.DeleteAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
	}
}
