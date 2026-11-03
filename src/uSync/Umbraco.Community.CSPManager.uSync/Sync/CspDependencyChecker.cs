using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.CSPManager.Models;
using uSync.Core.Dependency;

using UmbracoConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Community.CSPManager.uSync.Sync;

public class CspDependencyChecker : ISyncDependencyChecker<CspDefinition>
{
	// After content (1000) and domains (1100), so a push creates the node and its hostnames first.
	private static readonly int NodePolicyOrder = DependencyOrders.Domain + 50;

	private readonly IDomainService _domainService;

	public CspDependencyChecker(IDomainService domainService)
	{
		_domainService = domainService;
	}

	public UmbracoObjectTypes ObjectType => UmbracoObjectTypes.Unknown;

	public async Task<IEnumerable<uSyncDependency>> GetDependenciesAsync(CspDefinition item, DependencyFlags flags)
	{
		if (item == null) return [];

		List<uSyncDependency> dependencies =
		[
			new uSyncDependency
			{
				Name = CspItemNames.Name(item),
				Udi = CspItemNames.GetUdi(item),
				Order = item.ContentKey is null ? 11 : NodePolicyOrder,
				Flags = DependencyFlags.None,
				Level = 0
			}
		];

		if (item.ContentKey is not { } contentKey)
		{
			return dependencies;
		}

		// A node policy can only be created for a node with a hostname, so the target needs both.
		// The node takes the push's own flags (e.g. ancestors, dependencies) but never its children.
		dependencies.Add(new uSyncDependency
		{
			Name = $"Content node {contentKey:D}",
			Udi = Udi.Create(UmbracoConstants.UdiEntityType.Document, contentKey),
			Order = DependencyOrders.Content,
			Flags = flags & ~DependencyFlags.IncludeChildren,
			Level = 0
		});

		foreach (var domain in await _domainService.GetAssignedDomainsAsync(contentKey, includeWildcards: false))
		{
			dependencies.Add(new uSyncDependency
			{
				Name = domain.DomainName,
				// The entity type of uSync's Domain handler; Umbraco has no UdiEntityType constant for it.
				Udi = new GuidUdi("domain", domain.Key),
				Order = DependencyOrders.Domain,
				Flags = DependencyFlags.None,
				Level = 0
			});
		}

		return dependencies;
	}
}
