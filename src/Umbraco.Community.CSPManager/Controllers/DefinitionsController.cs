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
/// frontend and backoffice contexts, and to create, list and delete node policies. All
/// endpoints require authentication and authorization to the CSP Manager section.
/// </remarks>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Definitions")]
public class DefinitionsController : CspManagerControllerBase
{
	private readonly ICspService _cspService;
	private readonly CspDomainNodeLookup _domainNodes;
	private readonly ILanguageService _languageService;
	private readonly IOptionsMonitor<CspManagerOptions> _options;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefinitionsController"/> class.
	/// </summary>
	/// <param name="cspService">The CSP service for managing definitions.</param>
	/// <param name="domainService">The Umbraco domain service, to resolve the hostnames of a node.</param>
	/// <param name="entityService">The Umbraco entity service, to resolve content node names.</param>
	/// <param name="languageService">The Umbraco language service, to tell whether a policy's culture is still a language.</param>
	/// <param name="options">The CSP Manager options.</param>
	public DefinitionsController(
		ICspService cspService,
		IDomainService domainService,
		IEntityService entityService,
		ILanguageService languageService,
		IOptionsMonitor<CspManagerOptions> options)
	{
		_cspService = cspService;
		_domainNodes = new CspDomainNodeLookup(domainService, entityService);
		_languageService = languageService;
		_options = options;
	}

	/// <summary>
	/// Retrieves the global CSP definition for the specified context, or the node policy of a content node.
	/// </summary>
	/// <param name="isBackOffice">
	/// <c>true</c> to retrieve the backoffice CSP policy; <c>false</c> for the frontend policy.
	/// Defaults to <c>false</c>. Ignored when <paramref name="contentKey"/> is given.
	/// </param>
	/// <param name="contentKey">The key of a content node, to retrieve that node's policy.</param>
	/// <param name="culture">
	/// With a <paramref name="contentKey"/>: the ISO code of a culture, to retrieve the node's policy
	/// for that culture. Leave out for the node's policy for all cultures. No fallback is applied.
	/// </param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The <see cref="CspApiDefinition"/> for the specified context.</returns>
	/// <response code="404">A <paramref name="contentKey"/> was given and that node has no node policy for that culture.</response>
	[HttpGet("Definitions")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiDefinition>> GetDefinition(
		bool isBackOffice = false,
		Guid? contentKey = null,
		string? culture = null,
		CancellationToken cancellationToken = default)
	{
		if (contentKey is null)
		{
			return CspApiDefinition.FromCspDefinition(await _cspService.GetCspDefinitionAsync(isBackOffice, cancellationToken));
		}

		var definition = await _cspService.GetCspDefinitionForNodeAsync(contentKey.Value, culture, cancellationToken);
		return definition is null ? NotFound() : await ToApiDefinitionAsync(definition);
	}

	/// <summary>
	/// Retrieves the policy a frontend request for a content node gets today: its own policy, the
	/// nearest policy further up the content tree, or the global frontend policy, with the sources
	/// of every policy that extends it.
	/// </summary>
	/// <param name="contentKey">The key of the content node.</param>
	/// <param name="culture">The ISO code of the request's culture. Leave out for a request without a culture.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// The effective policy. Its id, content node and culture are those of the most specific policy
	/// that applies, so they say where the policy comes from; for the global frontend policy the
	/// content node is <c>null</c>.
	/// </returns>
	/// <response code="404">The content node doesn't exist or is in the recycle bin.</response>
	[HttpGet("Definitions/effective")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiDefinition>> GetEffectiveDefinition(
		[FromQuery] Guid contentKey,
		string? culture = null,
		CancellationToken cancellationToken = default)
	{
		if (!await _cspService.ContentNodeExistsAsync(contentKey, cancellationToken))
		{
			return NotFound();
		}

		var definition = await _cspService.ResolveCspDefinitionAsync(contentKey, domainContentKey: null, culture, cancellationToken)
			?? await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, cancellationToken);

		return await ToApiDefinitionAsync(definition);
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
	/// Lists every node policy, including orphaned ones whose node or language was deleted.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>
	/// The node policies, ordered by content node name and then culture (the policy for all cultures
	/// first); orphans last.
	/// </returns>
	[HttpGet("Definitions/node-policies")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspApiNodePolicy>), 200)]
	public async Task<ActionResult<IEnumerable<CspApiNodePolicy>>> GetNodePolicies(CancellationToken cancellationToken = default)
	{
		var policies = await _cspService.GetAllNodePoliciesAsync(cancellationToken);
		if (policies.Count == 0)
		{
			return Ok(Array.Empty<CspApiNodePolicy>());
		}

		// One query for the nodes of every policy. A node in the recycle bin still has a name.
		var nodes = _domainNodes.GetContentNodes(policies.Select(p => p.ContentKey!.Value));
		var languages = await GetLanguageIsoCodesAsync();

		var result = policies
			.Select(p =>
			{
				var contentKey = p.ContentKey!.Value;
				var node = nodes.GetValueOrDefault(contentKey);
				return new CspApiNodePolicy
				{
					Id = p.Id,
					ContentKey = contentKey,
					ContentName = node?.Name,
					Culture = p.Culture,
					Inheritance = p.Inheritance,
					AppliesTo = p.AppliesTo,
					Enabled = p.Enabled,
					IsOrphaned = node is null || node.Trashed || (p.Culture is not null && !languages.Contains(p.Culture)),
				};
			})
			.OrderBy(p => p.IsOrphaned)
			.ThenBy(p => p.ContentName, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.ContentKey)
			.ThenBy(p => p.Culture, StringComparer.OrdinalIgnoreCase)
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
	/// Saves global or node policies. Posting <see cref="Guid.Empty"/> with a content key creates a new policy with a server-assigned ID.
	/// </remarks>
	/// <response code="200">The definition was saved successfully.</response>
	/// <response code="400">The definition failed validation (e.g., duplicate sources, invalid ID, changed content node).</response>
	/// <response code="404">The definition is a node policy that doesn't exist.</response>
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

		// A node policy is created by posting Guid.Empty; any other id must already exist, so a
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
	/// Creates an enabled node policy for a content node.
	/// </summary>
	/// <param name="contentKey">The key of a content node that has no node policy for the culture yet.</param>
	/// <param name="culture">The ISO code of one of the site's languages. Leave out for a policy for all cultures.</param>
	/// <param name="inheritance">
	/// <see cref="CspPolicyInheritance.Replace"/> (the default) creates a complete policy, as a copy
	/// of the policy the node gets today (a policy further up the content tree, or the global frontend
	/// policy). <see cref="CspPolicyInheritance.Extend"/> creates a policy without sources, which
	/// keeps following that policy and adds its own sources to it.
	/// </param>
	/// <param name="appliesTo">
	/// <see cref="CspPolicyScope.Descendants"/> (the default) for the node and the pages below it;
	/// <see cref="CspPolicyScope.NodeOnly"/> for the node alone.
	/// </param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The new node policy.</returns>
	/// <response code="200">The node policy was created.</response>
	/// <response code="400">
	/// The node doesn't exist or is in the recycle bin, the culture isn't a language, or the node
	/// already has a policy for the culture.
	/// </response>
	[HttpPost("Definitions/create-from-frontend")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> CreateFromFrontend(
		[FromQuery] Guid contentKey,
		[FromQuery] string? culture = null,
		[FromQuery] CspPolicyInheritance inheritance = CspPolicyInheritance.Replace,
		[FromQuery] CspPolicyScope appliesTo = CspPolicyScope.Descendants,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var created = await _cspService.CreateCspDefinitionForNodeAsync(contentKey, culture, inheritance, appliesTo, cancellationToken);
			return Ok(await ToApiDefinitionAsync(created));
		}
		catch (CspDefinitionValidationException ex)
		{
			return CspValidationProblem(ex);
		}
	}

	/// <summary>
	/// Deletes a node policy. The global frontend and backoffice policies cannot be deleted.
	/// </summary>
	/// <param name="id">The id of the node policy.</param>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <response code="200">The node policy was deleted.</response>
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

		// The hostnames only say where the policy applies: most nodes with a policy have none.
		var domainNode = await _domainNodes.GetDomainNodeAsync(contentKey);
		return CspApiDefinition.FromCspDefinition(
			definition,
			domainNode?.Name ?? _domainNodes.GetContentNodes([contentKey]).GetValueOrDefault(contentKey)?.Name,
			domainNode?.Domains.Select(d => new CspApiDomainName { Name = d.Name, Culture = d.Culture }),
			_options.CurrentValue.DisabledNodePolicyBehavior);
	}

	private async Task<HashSet<string>> GetLanguageIsoCodesAsync()
		=> (await _languageService.GetAllAsync()).Select(l => l.IsoCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

	private BadRequestObjectResult CspValidationProblem(CspDefinitionValidationException ex)
	{
		ModelState.AddModelError(ex.MemberName, ex.Message);
		return BadRequest(new ValidationProblemDetails(ModelState));
	}
}
