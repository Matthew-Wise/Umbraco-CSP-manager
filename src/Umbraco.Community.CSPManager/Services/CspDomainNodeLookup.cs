using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.CSPManager.Services;

/// <summary>
/// Reads the content nodes a node policy can belong to: nodes with at least one hostname
/// (non-wildcard domain) in Culture and Hostnames, keyed by content key, with their hostnames.
/// </summary>
/// <remarks>
/// Policies are keyed on the node rather than on a hostname: Umbraco doesn't persist a key for
/// domains (<c>IDomain.Key</c> is a new <see cref="Guid"/> on every load), hostnames usually differ
/// per environment, and a multilingual site has one hostname per culture on the same node. A node
/// key is stable across restarts, environments (uSync) and hostname renames.
/// Not cached: it serves backoffice requests and saves only, which are rare.
/// </remarks>
internal sealed class CspDomainNodeLookup
{
	private readonly IDomainService _domainService;
	private readonly IEntityService _entityService;

	public CspDomainNodeLookup(IDomainService domainService, IEntityService entityService)
	{
		_domainService = domainService;
		_entityService = entityService;
	}

	/// <summary>
	/// Gets every node with a hostname, ordered by the sort order of its first hostname.
	/// </summary>
	public async Task<IReadOnlyList<DomainNode>> GetDomainNodesAsync()
	{
		var domains = UsableDomains(await _domainService.GetAllAsync(includeWildcards: false)).ToList();
		if (domains.Count == 0)
		{
			return [];
		}

		var nodeIds = domains.Select(d => d.RootContentId!.Value).Distinct().ToArray();
		var nodes = _entityService.GetAll(UmbracoObjectTypes.Document, nodeIds)
			.Where(IsUsable)
			.ToDictionary(e => e.Id);

		// GroupBy keeps the order of each group's first element, so the result follows the first hostname.
		return [.. domains
			.GroupBy(d => d.RootContentId!.Value)
			.Where(g => nodes.ContainsKey(g.Key))
			.Select(g => ToDomainNode(nodes[g.Key], g))];
	}

	/// <summary>
	/// Gets the node with the given key, or <c>null</c> when it doesn't exist, is in the recycle bin
	/// or has no hostname.
	/// </summary>
	public async Task<DomainNode?> GetDomainNodeAsync(Guid contentKey)
	{
		var domains = UsableDomains(await _domainService.GetAssignedDomainsAsync(contentKey, includeWildcards: false)).ToList();
		if (domains.Count == 0)
		{
			return null;
		}

		var node = _entityService.Get(contentKey, UmbracoObjectTypes.Document);
		return node is not null && IsUsable(node) ? ToDomainNode(node, domains) : null;
	}

	/// <summary>
	/// Whether a node policy can apply to the node: it exists, isn't in the recycle bin and has a hostname.
	/// </summary>
	public async Task<bool> HasHostnameAsync(Guid contentKey)
		=> await GetDomainNodeAsync(contentKey) is not null;

	/// <summary>
	/// Gets the names of content nodes whether or not they have a hostname, keyed by content key. A
	/// node that doesn't exist (e.g. for an orphaned policy whose node was deleted) is left out.
	/// </summary>
	public IReadOnlyDictionary<Guid, string?> GetContentNames(IEnumerable<Guid> contentKeys)
	{
		var keys = contentKeys.Distinct().ToArray();
		return keys.Length == 0
			? new Dictionary<Guid, string?>()
			: _entityService.GetAll(UmbracoObjectTypes.Document, keys).ToDictionary(e => e.Key, e => e.Name);
	}

	// Umbraco keeps a node's domains while it is in the recycle bin, but never routes a request to it.
	private static bool IsUsable(IEntitySlim node) => !node.Trashed;

	private static IEnumerable<IDomain> UsableDomains(IEnumerable<IDomain> domains)
		=> domains
			.Where(d => d.RootContentId.HasValue && !string.IsNullOrWhiteSpace(d.DomainName))
			.OrderBy(d => d.SortOrder);

	private static DomainNode ToDomainNode(IEntitySlim node, IEnumerable<IDomain> domains)
		=> new(node.Key, node.Name ?? string.Empty, [.. domains.Select(d => new DomainName(d.DomainName, d.LanguageIsoCode))]);

	internal sealed record DomainNode(Guid Key, string Name, IReadOnlyList<DomainName> Domains);

	internal sealed record DomainName(string Name, string? Culture);
}
