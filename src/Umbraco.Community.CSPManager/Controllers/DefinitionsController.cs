using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
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
	private readonly IDomainService _domainService;
	private readonly IIdKeyMap _idKeyMap;

	/// <summary>
	/// Initializes a new instance of the <see cref="DefinitionsController"/> class.
	/// </summary>
	public DefinitionsController(ICspService cspService, IDomainService domainService, IIdKeyMap idKeyMap)
	{
		_cspService = cspService;
		_domainService = domainService;
		_idKeyMap = idKeyMap;
	}

	/// <summary>
	/// Retrieves the CSP definition for the specified context or domain.
	/// </summary>
	/// <param name="isBackOffice">True to retrieve the backoffice policy; false for the frontend policy. Defaults to false.</param>
	/// <param name="domainKey">When provided, retrieves the domain-specific policy for this Umbraco domain Guid Key.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	[HttpGet("Definitions")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(404)]
	public async Task<ActionResult<CspApiDefinition>> GetDefinition(
		bool isBackOffice = false,
		Guid? domainKey = null,
		CancellationToken cancellationToken = default)
	{
		if (domainKey.HasValue)
		{
			var domainDefinition = await _cspService.GetCspDefinitionForDomainAsync(domainKey.Value, cancellationToken);
			if (domainDefinition is null)
			{
				return NotFound();
			}

			var (domainName, rootContentKey) = await ResolveDomainInfoAsync(domainKey.Value);
			return CspApiDefinition.FromCspDefinition(domainDefinition, domainName, rootContentKey);
		}

		return CspApiDefinition.FromCspDefinition(await _cspService.GetCspDefinitionAsync(isBackOffice, cancellationToken));
	}

	/// <summary>
	/// Retrieves all domain-specific CSP policies.
	/// </summary>
	[HttpGet("Definitions/domain-policies")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspApiDefinition>), 200)]
	public async Task<ActionResult<IEnumerable<CspApiDefinition>>> GetDomainPolicies(CancellationToken cancellationToken = default)
	{
		var policies = await _cspService.GetAllDomainPoliciesAsync(cancellationToken);
		var domainLookup = await BuildDomainLookupAsync();

		var result = policies.Select(p =>
		{
			string? domainName = null;
			Guid? rootContentKey = null;
			if (p.DomainKey.HasValue && domainLookup.TryGetValue(p.DomainKey.Value, out var info))
			{
				domainName = info.Name;
				rootContentKey = info.RootContentKey;
			}
			return CspApiDefinition.FromCspDefinition(p, domainName, rootContentKey);
		});

		return Ok(result);
	}

	/// <summary>
	/// Saves a CSP definition to the database.
	/// </summary>
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

		string? domainName = null;
		Guid? rootContentKey = null;
		if (savedDefinition.DomainKey.HasValue)
		{
			(domainName, rootContentKey) = await ResolveDomainInfoAsync(savedDefinition.DomainKey.Value);
		}

		return Ok(CspApiDefinition.FromCspDefinition(savedDefinition, domainName, rootContentKey));
	}

	/// <summary>
	/// Creates a new domain-specific CSP policy by copying the global frontend policy.
	/// </summary>
	/// <param name="domainKey">The Umbraco domain Guid Key to create the policy for.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	[HttpPost("Definitions/create-from-frontend")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(CspApiDefinition), 200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> CreateFromFrontend([FromQuery] Guid domainKey, CancellationToken cancellationToken = default)
	{
		// Check a policy doesn't already exist for this domain
		var existing = await _cspService.GetCspDefinitionForDomainAsync(domainKey, cancellationToken);
		if (existing is not null)
		{
			ModelState.AddModelError(nameof(domainKey), "A CSP policy already exists for this domain.");
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		// Copy the global frontend policy
		var frontend = await _cspService.GetCspDefinitionAsync(isBackOfficeRequest: false, cancellationToken);

		var newDefinition = new Models.CspDefinition
		{
			Id = Guid.NewGuid(),
			Enabled = frontend.Enabled,
			IsBackOffice = false,
			ReportOnly = frontend.ReportOnly,
			ReportingDirective = frontend.ReportingDirective,
			ReportUri = frontend.ReportUri,
			UpgradeInsecureRequests = frontend.UpgradeInsecureRequests,
			DomainKey = domainKey,
			Sources = frontend.Sources.Select(s => new Models.CspDefinitionSource
			{
				Source = s.Source,
				Directives = [.. s.Directives]
			}).ToList()
		};

		// Fix up DefinitionId references
		foreach (var source in newDefinition.Sources)
		{
			source.DefinitionId = newDefinition.Id;
		}

		var saved = await _cspService.SaveCspDefinitionAsync(newDefinition, cancellationToken);
		var (domainName, rootContentKey) = await ResolveDomainInfoAsync(domainKey);

		return Ok(CspApiDefinition.FromCspDefinition(saved, domainName, rootContentKey));
	}

	/// <summary>
	/// Deletes a domain-specific CSP policy. Global policies cannot be deleted.
	/// </summary>
	/// <param name="id">The CSP definition ID to delete.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	[HttpDelete("Definitions/{id:guid}")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(200)]
	[ProducesResponseType(typeof(ProblemDetails), 400)]
	public async Task<IActionResult> DeleteDefinition(Guid id, CancellationToken cancellationToken = default)
	{
		if (id == Constants.DefaultBackofficeId || id == Constants.DefaultFrontEndId)
		{
			ModelState.AddModelError(nameof(id), "Global CSP policies cannot be deleted.");
			return BadRequest(new ValidationProblemDetails(ModelState));
		}

		await _cspService.DeleteCspDefinitionAsync(id, cancellationToken);
		return Ok();
	}

	private sealed record DomainInfo(string? Name, Guid? RootContentKey);

	private async Task<IReadOnlyDictionary<Guid, DomainInfo>> BuildDomainLookupAsync()
	{
		var domains = await _domainService.GetAllAsync(includeWildcards: false);
		return domains.ToDictionary(d => d.Key, d =>
		{
			Guid? rootContentKey = null;
			if (d.RootContentId.HasValue)
			{
				var attempt = _idKeyMap.GetKeyForId(d.RootContentId.Value, UmbracoObjectTypes.Document);
				if (attempt.Success)
				{
					rootContentKey = attempt.Result;
				}
			}
			return new DomainInfo(d.DomainName, rootContentKey);
		});
	}

	private async Task<(string? DomainName, Guid? RootContentKey)> ResolveDomainInfoAsync(Guid domainKey)
	{
		var lookup = await BuildDomainLookupAsync();
		return lookup.TryGetValue(domainKey, out var info)
			? (info.Name, info.RootContentKey)
			: (null, null);
	}
}
