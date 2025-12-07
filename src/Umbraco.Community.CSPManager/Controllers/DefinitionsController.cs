using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller for managing Content Security Policy definitions.
/// </summary>
/// <remarks>
/// This controller provides endpoints to retrieve and save CSP definitions for both
/// frontend and backoffice contexts. All endpoints require authentication and
/// authorization to the CSP Manager section.
/// </remarks>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController : CspManagerControllerBase
{
	private readonly ICspService _cspService;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefinitionsController"/> class.
	/// </summary>
	/// <param name="cspService">The CSP service for managing definitions.</param>
	public DefinitionsController(ICspService cspService)
	{
		_cspService = cspService;
	}

	/// <summary>
	/// Retrieves the CSP definition for the specified context.
	/// </summary>
	/// <param name="isBackOffice">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// Defaults to <c>false</c>.
	/// </param>
	/// <param name="_">Cancellation token (unused, required for test infrastructure).</param>
	/// <returns>The <see cref="CspApiDefinition"/> for the specified context.</returns>
	[HttpGet("Definitions")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	public ActionResult<CspApiDefinition> GetDefinition(bool isBackOffice = false, CancellationToken _ = default)
		=> CspApiDefinition.FromCspDefinition(_cspService.GetCspDefinition(isBackOffice));

	/// <summary>
	/// Saves a CSP definition to the database.
	/// </summary>
	/// <param name="definition">The CSP definition to save.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// An <see cref="IActionResult"/> containing the saved definition on success,
	/// or a validation problem details object if the model state is invalid.
	/// </returns>
	/// <response code="200">The definition was saved successfully.</response>
	/// <response code="400">The definition failed validation (e.g., duplicate sources, invalid ID).</response>
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

		var savedDefinition = await _cspService.SaveCspDefinitionAsync(definition.ToCspDefinition(), cancellationToken);
		return Ok(savedDefinition);
	}
}