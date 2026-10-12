using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller for managing Content Security Policy definitions.
/// </summary>
/// <remarks>
/// This controller provides endpoints to retrieve and save CSP definitions for both
/// frontend and backoffice contexts, and to create, list and delete domain policies. All
/// endpoints require authentication and authorization to the CSP Manager section.
/// </remarks>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController : CspManagerControllerBase
{
	private readonly ICspService _cspService;
	private readonly CspDomainNodeLookup _domainNodes;
	private readonly IOptionsMonitor<CspManagerOptions> _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefinitionsController"/> class.
	/// </summary>
	/// <param name="cspService">The CSP service for managing definitions.</param>
	/// <param name="domainService">The Umbraco domain service, to resolve the hostnames of a node.</param>
	/// <param name="entityService">The Umbraco entity service, to resolve content node names.</param>
	/// <param name="options">The CSP Manager options.</param>
	public DefinitionsController(
		ICspService cspService,
		IDomainService domainService,
		IEntityService entityService,
		IOptionsMonitor<CspManagerOptions> options)
	{
		_cspService = cspService;
		_domainNodes = new CspDomainNodeLookup(domainService, entityService);
		_options = options;
	}

	/// <summary>
	/// Retrieves the global CSP definition for the specified context, or the domain policy of a content node.
	/// </summary>
	/// <param name="isBackOffice">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// Defaults to <c>false</c>. Ignored when <paramref name="contentKey"/> is given.
	/// </param>
	/// <param name="contentKey">The key of a content node, to retrieve that node's domain policy.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The <see cref="CspApiDefinition"/> for the specified context.</returns>
	/// <response code="404">A <paramref name="contentKey"/> was given and that node has no domain policy.</response>
	[HttpGet("Definitions")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiDefinition>> GetDefinition(
		bool isBackOffice = false,
		Guid? contentKey = null,
		CancellationToken cancellationToken = default)
	{
		if (contentKey is null)
		{
			return CspApiDefinition.FromCspDefinition(await _cspService.GetCspDefinitionAsync(isBackOffice, cancellationToken));
		}

		var definition = await _cspService.GetCspDefinitionForDomainAsync(contentKey.Value, cancellationToken);
		return definition is null ? NotFound() : await ToApiDefinitionAsync(definition);
	}

	/// <summary>
	/// Retrieves a CSP definition by its id.
	/// </summary>
	/// <param name="id">The definition id.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The definition.</returns>
	/// <response code="404">There is no definition with that id.</response>
	[HttpGet("Definitions/{id:guid}")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiDefinition>> GetDefinitionById(Guid id, CancellationToken cancellationToken = default)
	{
		var definition = await _cspService.GetCspDefinitionAsync(id, cancellationToken);
		return definition is null ? NotFound() : await ToApiDefinitionAsync(definition);
	}

	/// <summary>
	/// Lists every domain policy, including orphaned ones whose node was deleted or lost its hostnames.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The domain policies, ordered by content node name; orphans last.</returns>
	[HttpGet("Definitions/domain-policies")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspApiDomainPolicy>), 200)]
	public async Task<ActionResult<IEnumerable<CspApiDomainPolicy>>> GetDomainPolicies(CancellationToken cancellationToken = default)
	{
		var policies = await _cspService.GetAllDomainPoliciesAsync(cancellationToken);
		var nodes = (await _domainNodes.GetDomainNodesAsync()).ToDictionary(n => n.Key);

		// An orphan's node may still exist without a hostname (or in the recycle bin), so its name is
		// looked up separately, in one query for all orphans.
		var orphanNames = _domainNodes.GetContentNames(
			policies.Select(p => p.ContentKey!.Value).Where(k => !nodes.ContainsKey(k)));

		var result = policies
			.Select(p =>
			{
				var contentKey = p.ContentKey!.Value;
				var node = nodes.GetValueOrDefault(contentKey);
				return new CspApiDomainPolicy
				{
					Id = p.Id,
					ContentKey = contentKey,
					ContentName = node?.Name ?? orphanNames.GetValueOrDefault(contentKey),
					Enabled = p.Enabled,
					IsOrphaned = node is null,
				};
			})
			.OrderBy(p => p.IsOrphaned)
			.ThenBy(p => p.ContentName, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return Ok(result);
	}

	/// <summary>
	/// Saves a CSP definition to the database.
	/// </summary>
	/// <param name="definition">The CSP definition to save.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// An <see cref="IActionResult"/> containing the saved definition on success,
	/// or a validation problem details object if the model state is invalid.
	/// </returns>
	/// <remarks>
	/// Saves global or domain policies. Posting <see cref="Guid.Empty"/> with a content key creates a new policy with a server-assigned ID.
	/// </remarks>
	/// <response code="200">The definition was saved successfully.</response>
	/// <response code="400">The definition failed validation (e.g., duplicate sources, invalid ID, changed content node).</response>
	/// <response code="404">The definition is a domain policy that doesn't exist.</response>
	[HttpPost("Definitions/save")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> SaveDefinition([FromBody] CspApiDefinition definition, CancellationToken cancellationToken = default)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		// A domain policy is created by posting Guid.Empty; any other id must already exist, so a
		// caller can't create a policy under an id of its choosing.
		if (definition.ContentKey is not null
			&& definition.Id != Guid.Empty
			&& await _cspService.GetCspDefinitionAsync(definition.Id, cancellationToken) is null)
		{
			return NotFound();
		}

		try
		{
			var savedDefinition = await _cspService.SaveCspDefinitionAsync(definition.ToCspDefinition(), cancellationToken);
			return Ok(await ToApiDefinitionAsync(savedDefinition));
		}
		catch (CspDefinitionValidationException ex)
		{
			return CspValidationProblem(ex);
		}
	}

	/// <summary>
	/// Creates a domain policy for a content node as an enabled copy of the global frontend policy.
	/// </summary>
	/// <param name="contentKey">The key of a content node with a hostname (non-wildcard domain) that has no domain policy yet.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The new domain policy.</returns>
	/// <response code="200">The domain policy was created.</response>
	/// <response code="400">The node doesn't exist, has no hostname, or already has a domain policy.</response>
	[HttpPost("Definitions/create-from-frontend")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> CreateFromFrontend([FromQuery] Guid contentKey, CancellationToken cancellationToken = default)
	{
		try
		{
			var created = await _cspService.CreateCspDefinitionForDomainAsync(contentKey, cancellationToken);
			return Ok(await ToApiDefinitionAsync(created));
		}
		catch (CspDefinitionValidationException ex)
		{
			return CspValidationProblem(ex);
		}
	}

	/// <summary>
	/// Deletes a domain policy. The global frontend and backoffice policies cannot be deleted.
	/// </summary>
	/// <param name="id">The id of the domain policy.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <response code="200">The domain policy was deleted.</response>
	/// <response code="400">The id is one of the global policies.</response>
	/// <response code="404">There is no definition with that id.</response>
	[HttpDelete("Definitions/{id:guid}")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> DeleteDefinition(Guid id, CancellationToken cancellationToken = default)
	{
		if (id != Constants.DefaultFrontEndId && id != Constants.DefaultBackofficeId
			&& await _cspService.GetCspDefinitionAsync(id, cancellationToken) is null)
		{
			return NotFound();
		}

		try
		{
			await _cspService.DeleteCspDefinitionAsync(id, cancellationToken);
			return Ok();
		}
		catch (CspDefinitionValidationException ex)
		{
			return CspValidationProblem(ex);
		}
	}

	private async Task<CspApiDefinition> ToApiDefinitionAsync(CspDefinition definition)
	{
		if (definition.ContentKey is not { } contentKey)
		{
			return CspApiDefinition.FromCspDefinition(definition);
		}

		var node = await _domainNodes.GetDomainNodeAsync(contentKey);
		return CspApiDefinition.FromCspDefinition(
			definition,
			node?.Name ?? _domainNodes.GetContentNames([contentKey]).GetValueOrDefault(contentKey),
			node?.Domains.Select(d => new CspApiDomainName { Name = d.Name, Culture = d.Culture }),
			_options.CurrentValue.DisabledDomainPolicyBehavior);
	}

	private BadRequestObjectResult CspValidationProblem(CspDefinitionValidationException ex)
	{
		ModelState.AddModelError(ex.MemberName, ex.Message);
		return BadRequest(new ValidationProblemDetails(ModelState));
	}
}
