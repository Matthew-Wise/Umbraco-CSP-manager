using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller for retrieving Umbraco domain information with CSP policy status.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Domains")]
public class CspDomainsController : CspManagerControllerBase
{
	private readonly IDomainService _domainService;
	private readonly ICspService _cspService;

	public CspDomainsController(IDomainService domainService, ICspService cspService)
	{
		_domainService = domainService;
		_cspService = cspService;
	}

	/// <summary>
	/// Returns all Umbraco domains with an indicator of whether each has a CSP policy configured.
	/// </summary>
	[HttpGet("Domains")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspDomainInfo>), 200)]
	public async Task<ActionResult<IEnumerable<CspDomainInfo>>> GetDomains(CancellationToken cancellationToken = default)
	{
		var domains = await _domainService.GetAllAsync(includeWildcards: false);
		var domainPolicies = await _cspService.GetAllDomainPoliciesAsync(cancellationToken);

		var policyByDomainKey = domainPolicies
			.Where(p => p.DomainKey.HasValue)
			.ToDictionary(p => p.DomainKey!.Value, p => p.Id);

		var result = domains.Select(d => new CspDomainInfo
		{
			Key = d.Key,
			Name = d.DomainName ?? string.Empty,
			HasCspPolicy = policyByDomainKey.ContainsKey(d.Key),
			CspDefinitionId = policyByDomainKey.TryGetValue(d.Key, out var defId) ? defId : null,
		});

		return Ok(result);
	}
}
