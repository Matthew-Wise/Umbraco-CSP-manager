using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

[ApiVersion("1.0")]
[MapToApi(Constants.ApiName)]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController : ManagementApiControllerBase
{
	private readonly ICspService _cspService;

	public DefinitionsController(ICspService cspService)
	{
		_cspService = cspService;
	}

	[HttpGet("Definitions")]
	[ProducesResponseType(typeof(CspDefinition), 200)]
	public ActionResult<CspDefinition> GetDefinition(bool isBackOffice = false) => _cspService.GetCspDefinition(isBackOffice);

	[HttpPost("Definitions/save")]
	[ProducesResponseType(typeof(CspDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> SaveDefinition(CspDefinition definition)
	{
		if (definition.Id == Guid.Empty)
		{
			return BadRequest(new ProblemDetailsBuilder()
				.WithTitle("Invalid Definition")
				.WithDetail("Definition Id is blank")
				.Build());
		}

		var savedDefinition = await _cspService.SaveCspDefinitionAsync(definition);
		return Ok(savedDefinition);
	}
}