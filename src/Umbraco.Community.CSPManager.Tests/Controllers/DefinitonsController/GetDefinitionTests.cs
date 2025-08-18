using System.Linq.Expressions;
using System.Net;
using Umbraco.Cms.Core.Models;
using Umbraco.Community.CSPManager.Controllers;
using UmbConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.Tests.Controllers.DefinitonsController;
internal class GetDefinitionTests : CspManagementApiTest<DefinitionsController>
{
	protected override Expression<Func<DefinitionsController, object>> MethodSelector => x => x.GetDefinition(true);
	
	[Test]
	public async Task GetDefinition_WithOutSectionAcess_Returns_Forbidden()
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

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
	}

	[Test]
	public async Task GetDefinition_WithSectionAces_Returns_Ok()
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

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
	}


	[Test]
	public async Task GetDefinition_Returns_KnownDefaultBackOfficeCSP()
	{
		await AuthenticateClientAsync(Client, "admin@example.com", "1234567890", true);

		var url = GetManagementApiUrl(MethodSelector);
		var result = await Client.GetAsync(url);
		await VerifyJson(await result.Content.ReadAsStringAsync(), VerifySettings);
	}
}
