using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.CSPManager.Controllers;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Tests.Controllers;
public class DefinitionsControllerTests
{

	[Test]
	public async Task SaveDefinition_ReturnsBadRequest_WhenIdIsEmpty()
	{
		var serviceMock = new Mock<ICspService>();
		var controller = new DefinitionsController(serviceMock.Object);

		var definition = new CspDefinition { Id = Guid.Empty };
		var result = await controller.SaveDefinition(definition);

		Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());

		var badRequestResult = result as BadRequestObjectResult;
		Assert.That(badRequestResult.Value, Is.InstanceOf<ProblemDetails>());
	}
}
