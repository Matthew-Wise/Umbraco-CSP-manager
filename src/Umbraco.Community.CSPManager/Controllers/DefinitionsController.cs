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
	// Has a CancellationToken parameter to allow MethodSelector to work correctly for testing
	public ActionResult<CspApiDefinition> GetDefinition(bool isBackOffice = false, CancellationToken _ = default)
		=> CspApiDefinition.FromCspDefiniton(_cspService.GetCspDefinition(isBackOffice));

	[HttpPost("Definitions/save")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> SaveDefinition([FromBody] CspApiDefinition definition, CancellationToken cancellationToken = default)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		var savedDefinition = await _cspService.SaveCspDefinitionAsync(definition.ToCspDefiniton(), cancellationToken);
		return Ok(savedDefinition);
	}
}