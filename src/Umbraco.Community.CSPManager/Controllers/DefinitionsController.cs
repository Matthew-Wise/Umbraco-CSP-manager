using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;


[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController : CspManagerControllerBase
{
	private readonly ICspService _cspService;

	public DefinitionsController(ICspService cspService)
	{
		_cspService = cspService;
	}

	[HttpGet("Definitions")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	public ActionResult<CspApiDefinition> GetDefinition(bool isBackOffice = false)
		=> CspApiDefinition.FromCspDefiniton(_cspService.GetCspDefinition(isBackOffice));

	[HttpPost("Definitions/save")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> SaveDefinition([FromBody] CspApiDefinition definition)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		var savedDefinition = await _cspService.SaveCspDefinitionAsync(definition.ToCspDefiniton());
		return Ok(savedDefinition);
	}
}