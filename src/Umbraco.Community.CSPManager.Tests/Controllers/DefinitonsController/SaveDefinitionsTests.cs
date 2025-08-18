using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Models.Api;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerFixture)]
public class SaveDefinitionsTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector => x => x.SaveDefinition(new());

	[Test]
	public async Task SaveDefinition_ReturnsBadRequest_WhenIdIsEmpty()
	{

		await AuthenticateClientAsync(Client, "admin@example.com", "1234567890", true);

		var definition = JsonContent.Create<CspApiDefinition>(new()
		{
			Id = Guid.Empty
		});

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, definition);

		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		await VerifyJson(await result.Content.ReadAsStringAsync(), VerifySettings);
	}


	[Test]
	public async Task SaveDefinition_ReturnsBadRequest_WithDuplicateSourceNames()
	{
		await AuthenticateClientAsync(Client, "admin@example.com", "1234567890", true);

		var definition = JsonContent.Create<CspApiDefinition>(new() { Id = Constants.DefaultBackofficeId, Sources = [new() { Source = "test" }, new() { Source = "test" }] });


		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, definition);

		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		await VerifyJson(await result.Content.ReadAsStringAsync(), VerifySettings);
	}

	[Test]
	public async Task SaveDefinition_WithOutSectionAcess_Returns_Forbidden()
	{
		await AuthenticateClientAsync(Client, async userService =>
		{
			var email = "test@test.com";
			var testUserCreateModel = new UserCreateModel
			{
				Email = email,
				Name = "Test Mc.Gee",
				UserName = email,
				UserGroupKeys = new HashSet<Guid> { UmbConstants.Security.EditorGroupKey },
			};

			var userCreationResult = await userService.CreateAsync(UmbConstants.Security.SuperUserKey, testUserCreateModel, true);

			Assert.That(userCreationResult.Success, Is.True);
			return (userCreationResult.Result.CreatedUser, "1234567890");
		});


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
		await AuthenticateClientAsync(Client, async userService =>
		{
			var email = "valid@test.com";
			var testUserCreateModel = new UserCreateModel
			{
				Email = email,
				Name = "Test Mc.Gee",
				UserName = email,
				UserGroupKeys = new HashSet<Guid> { userGroup.Key },
			};

			var userCreationResult = await userService.CreateAsync(UmbConstants.Security.SuperUserKey, testUserCreateModel, true);

			Assert.That(userCreationResult.Success, Is.True);
			return (userCreationResult.Result.CreatedUser, "1234567890");
		});


		var definition = JsonContent.Create<CspApiDefinition>(new()
		{
			Id = Constants.DefaultBackofficeId,
		});

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.PostAsync(url, definition);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}
}
