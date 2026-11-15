using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models.Api;
using Umbraco.Community.CSPManager.Services;

namespace Umbraco.Community.CSPManager.Controllers;

/// <summary>
/// API controller listing the content nodes with at least one hostname in Culture &amp; Hostnames:
/// the sites. Any content node can have a node policy; these are the usual ones to give one.
/// </summary>
[ApiVersion("1.0")]
[ApiExplorerSettings(GroupName = "Nodes")]
public class CspNodesController : CspManagerControllerBase
{
	private readonly ICspService _cspService;
	private readonly CspDomainNodeLookup _domainNodes;

	/// <summary>
	/// Initializes a new instance of the <see cref="CspNodesController"/> class.
	/// </summary>
	/// <param name="cspService">The CSP service, to find which nodes have a policy.</param>
	/// <param name="domainService">The Umbraco domain service.</param>
	/// <param name="entityService">The Umbraco entity service, to resolve content node names.</param>
	public CspNodesController(ICspService cspService, IDomainService domainService, IEntityService entityService)
	{
		_cspService = cspService;
		_domainNodes = new CspDomainNodeLookup(domainService, entityService);
	}

	/// <summary>
	/// Lists every content node with a hostname, with its hostnames and whether it has a node policy
	/// for all cultures.
	/// </summary>
	/// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>The nodes, ordered by the sort order of their first hostname.</returns>
	[HttpGet("Nodes")]
	[MapToApiVersion("1.0")]
	[ProducesResponseType(typeof(IEnumerable<CspApiNodeInfo>), 200)]
	public async Task<ActionResult<IEnumerable<CspApiNodeInfo>>> GetNodes(CancellationToken cancellationToken = default)
	{
		var nodes = await _domainNodes.GetDomainNodesAsync();
		// A node can have a policy per culture as well; this lists its policy for all cultures.
		var policies = (await _cspService.GetAllNodePoliciesAsync(cancellationToken))
			.Where(p => p.Culture is null)
			.ToDictionary(p => p.ContentKey!.Value, p => p.Id);

		var result = nodes.Select(n => new CspApiNodeInfo
		{
			ContentKey = n.Key,
			ContentName = n.Name,
			Domains = [.. n.Domains.Select(d => new CspApiDomainName { Name = d.Name, Culture = d.Culture })],
			HasCspPolicy = policies.ContainsKey(n.Key),
			CspDefinitionId = policies.TryGetValue(n.Key, out var id) ? id : null,
		}).ToList();

		return Ok(result);
	}
}
